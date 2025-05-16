using Dynamicweb.Core;
using Dynamicweb.Ecommerce.Cart;
using Dynamicweb.Ecommerce.International;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models;
using Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.Rates;
using Dynamicweb.Ecommerce.ShippingProviders.FedEx.Service;
using Dynamicweb.Extensibility.AddIns;
using Dynamicweb.Extensibility.Editors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx;

/// <summary>
/// FedEx Shipping Service
/// </summary>
[AddInName("FedEx (Beta)"), AddInDescription("FedEx Shipping Provider")]
public class FedEx : ShippingProvider, IParameterOptions, IDynamicParameterOptions
{
    #region "Parameters"

    [AddInParameter("Api key"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
    public string ApiKey { get; set; } = "";

    [AddInParameter("Secret key"), AddInParameterEditor(typeof(TextParameterEditor), "size=80; password=true")]
    public string SecretKey { get; set; } = "";

    [AddInParameter("Account Number"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
    public string AccountNumber { get; set; } = "";

    [AddInParameter("Service Type"), AddInParameterEditor(typeof(DropDownParameterEditor), "SortBy=Value"),
        AddInDescription("Identifies the FedEx service to use in shipping the package for a rate request.")]
    public string ServiceType { get; set; } = "";

    [AddInParameter("Pickup Type"), AddInParameterEditor(typeof(DropDownParameterEditor), "SortBy=Value"),
        AddInDescription("Identifies the method by which the package is to be tendered to FedEx. This element does not dispatch a courier for package pickup.")]
    public string PickupType { get; set; } = "";

    [AddInParameter("Packing Type"), AddInParameterEditor(typeof(DropDownParameterEditor), "SortBy=Value"),
        AddInDescription("Identifies the packaging used by the requester for the package.")]
    public string PackingType { get; set; } = "";

    [AddInParameter("Origination Street Address"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
    public string ShipperStreet { get; set; } = "";

    [AddInParameter("Origination Street Address 2"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
    public string ShipperStreet2 { get; set; } = "";

    [AddInParameter("Origination City"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
    public string ShipperCity { get; set; } = "";

    [AddInParameter("Origination Country"), AddInParameterEditor(typeof(DropDownParameterEditor), "SortBy=Value; reloadOnChange=true")]
    public string ShipperCountryCode { get; set; } = "";

    [AddInParameter("Origination State/Region"), AddInParameterEditor(typeof(DropDownParameterEditor), "SortBy=Value; dynamicOptions=true")]
    public string ShipperStateOrProvinceCode1 { get; set; } = "";

    [AddInParameter("Origination State/Region 2"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
    public string ShipperStateOrProvinceCode2 { get; set; } = "";

    [AddInParameter("Origination Zip Code"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
    public string ShipperPostalCode { get; set; } = "";  

    [AddInParameter("Use Residential Rates"), AddInParameterEditor(typeof(YesNoParameterEditor), "")]
    public bool UseResidentialRates { get; set; }

    [AddInParameter("Debug"), AddInParameterEditor(typeof(YesNoParameterEditor), ""), AddInDescription("Create a log of the request and response from FedEx")]
    public bool DebugLog { get; set; }

    [AddInParameter("Use LB instead og KG"), AddInParameterEditor(typeof(YesNoParameterEditor), ""), AddInDescription("Calculates shipping costs based on weights in LB")]
    public bool UseLbInsteadOfKg { get; set; }

    [AddInParameter("Test mode"), AddInParameterEditor(typeof(YesNoParameterEditor), "infoText=Set to use sandbox (test mode) for the API requests. Uncheck when ready for production.")]
    public bool TestMode { get; set; }

    #endregion

    //Locales supported by FedEx: https://developer.fedex.com/api/en-us/guides/api-reference.html#locales
    private static string[] SupportedLocales { get; set; } =
    [
        "ar_AE", "bg_BG", "zh_CN",
        "zh_HK", "zh_TW", "cs_CZ",
        "da_DK", "nl_NL", "en_CA",
        "en_GB", "en_US", "et_EE",
        "fi_FI", "fr_CA", "fr_FR",
        "de_DE", "de_CH", "el_GR",
        "hu_HU", "it_IT", "ja_JP",
        "ko_KR", "lv_LV", "lt_LT",
        "no_NO", "pl_PL", "pt_BR",
        "pt_PT", "ro_RO", "ru_RU",
        "sk_SK", "sl_SI", "es_AR",
        "es_MX", "es_ES", "es_US",
        "sv_SE", "th_TH", "tr_TR",
        "uk_UA", "vi_VN"
    ];

    private static string Locale
    {
        get
        {
            string currentLocale = Environment.ExecutingContext.GetCulture(true).Name.Replace('-', '_');
            if (!SupportedLocales.Contains(currentLocale))
                return "en_US";

            return currentLocale;
        }
    }

    private FedExService? service;
    private FedExService Service
    {
        get
        {
            service ??= new()
            {
                ApiKey = ApiKey,
                SecretKey = SecretKey,
                DebugLog = DebugLog,
                Locale = Locale,
                TestMode = TestMode
            };

            return service;
        }
    }

    /// <summary>
    /// Calculate shipping fee for the specified order
    /// </summary>
    /// <param name="Order">The order.</param>
    /// <returns>Returns shipping fee for the specified order</returns>
    public override PriceRaw CalculateShippingFee(Order order)
    {
        string requestCacheKey = order.AutoId.ToString();
        double rate = 0;
        order.ShippingProviderErrors.Clear();
        order.ShippingProviderWarnings.Clear();

        try
        {
            if (IsRequestParametersCorrect(order))
            {
                var rateRequest = ShippingProviderHelper.CheckIsRateRequestCached(ShippingID, requestCacheKey);
                if (rateRequest.Rate > 0 || ShippingProviderHelper.IsThisShippingRequestWasProcessed(ShippingID))
                {
                    rate = rateRequest.Rate;
                    if (rateRequest.Warning?.Any() is true)
                        order.ShippingProviderWarnings.AddRange(rateRequest.Warning);

                    if (rateRequest.Errors?.Any() is true)
                        order.ShippingProviderErrors.AddRange(rateRequest.Errors);
                }
                else
                {
                    RateAndTransitTimesResponse? response = Service.GetRateAndTransitTimes(order, this);
                    if (response is null)
                        throw new NullReferenceException("Rate and transit times request returns null. Please, see logs to find the problem.");

                    rate = ProcessResponse(response, order);
                }
            }
        }
        catch (Exception err)
        {
            order.ShippingProviderErrors.Add(err.Message);
        }

        ShippingProviderHelper.CacheRateRequest(ShippingID, requestCacheKey, rate, order.CurrencyCode, order.ShippingProviderWarnings, order.ShippingProviderErrors);
        ShippingProviderHelper.SetShippingRequestIsProcessed(ShippingID);

        return new PriceRaw(rate, order.Currency);
    }

    private bool IsRequestParametersCorrect(Order order)
    {
        if (string.IsNullOrEmpty(order.DeliveryZip) && string.IsNullOrEmpty(order.CustomerZip))
            order.ShippingProviderErrors.Add("ZipCode field is empty.");

        if (string.IsNullOrEmpty(order.DeliveryAddress) &&
            string.IsNullOrEmpty(order.DeliveryAddress2) &&
            string.IsNullOrEmpty(order.CustomerAddress) &&
            string.IsNullOrEmpty(order.CustomerAddress2))
        {
            order.ShippingProviderErrors.Add("Delivery address is empty.");
        }

        return order.ShippingProviderErrors.Count == 0;
    }

    private double ProcessResponse(RateAndTransitTimesResponse response, Order order)
    {
        if (response.Output?.Alerts?.Any() is true)
        {
            string alertsText = GetAlertsText(response.Output.Alerts);
            order.ShippingProviderWarnings.Add(alertsText);
        }

        foreach (var rateReplyDetail in response.Output?.RateReplyDetails ?? [])
        {
            foreach (var shipmentDetail in rateReplyDetail.RatedShipmentDetails ?? [])
            {
                if (shipmentDetail.TotalNetCharge > 0)
                    return shipmentDetail.TotalNetCharge;
            }
        }

        return 0;
    }

    private string GetAlertsText(IEnumerable<Alert> alerts)
    {
        var stringBuilder = new StringBuilder("Alerts");

        for (int i = 0; i < alerts.Count() - 1; i++)
        {
            var notification = alerts.ElementAt(i);
            stringBuilder.AppendLine($"Alert no. {i}");
            stringBuilder.AppendLine($"Code: {notification.Code}");
            stringBuilder.AppendLine($"Message: {notification.Message}");
            stringBuilder.AppendLine($"Type: {notification.AlertType}");
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Retrieves options
    /// </summary>
    /// <param name="optionName">Service Type, Dropoff Type, Packing Type, Origination State/Region or Origination Country</param>
    public IEnumerable<ParameterOption> GetParameterOptions(string parameterName)
    {
        string languageId = Services.Languages.GetDefaultLanguageId();

        var options = new List<ParameterOption>();

        switch (parameterName)
        {
            case "Service Type":
                options.Add(new("FedEx International Priority Express", "FEDEX_INTERNATIONAL_PRIORITY_EXPRESS"));
                options.Add(new("FedEx International First", "INTERNATIONAL_FIRST"));
                options.Add(new("FedEx International Priority", "FEDEX_INTERNATIONAL_PRIORITY"));
                options.Add(new("FedEx International Economy", "INTERNATIONAL_ECONOMY"));
                options.Add(new("FedEx International Ground and FedEx Domestic Ground®", "FEDEX_GROUND"));
                options.Add(new("FedEx First Overnight", "FIRST_OVERNIGHT"));
                options.Add(new("FedEx First Overnight Freight", "FEDEX_FIRST_FREIGHT"));
                options.Add(new("FedEx 1Day Freight (Hawaii service is to and from the island of Oahu only)", "FEDEX_1_DAY_FREIGHT"));
                options.Add(new("FedEx 2Day Freight (Hawaii service is to and from the island of Oahu only)", "FEDEX_2_DAY_FREIGHT"));
                options.Add(new("FedEx 3Day Freight (Except Alaska and Hawaii)", "FEDEX_3_DAY_FREIGHT"));
                options.Add(new("FedEx International Priority Freight", "INTERNATIONAL_PRIORITY_FREIGHT"));
                options.Add(new("FedEx International Economy Freight", "INTERNATIONAL_ECONOMY_FREIGHT"));
                options.Add(new("FedEx International Deferred Freight", "FEDEX_INTERNATIONAL_DEFERRED_FREIGHT"));
                options.Add(new("FedEx International Priority DirectDistribution®", "INTERNATIONAL_PRIORITY_DISTRIBUTION"));
                options.Add(new("FedEx International Priority DirectDistribution® Freight", "INTERNATIONAL_DISTRIBUTION_FREIGHT"));
                options.Add(new("International Ground Distribution (IGD)", "INTL_GROUND_DISTRIBUTION"));
                options.Add(new("FedEx Home Delivery", "GROUND_HOME_DELIVERY"));
                options.Add(new("FedEx Ground Economy (Formerly known as FedEx SmartPost®)", "SMART_POST"));
                options.Add(new("FedEx Priority Overnight", "PRIORITY_OVERNIGHT"));
                options.Add(new("FedEx Standard Overnight (Hawaii outbound only)", "STANDARD_OVERNIGHT"));
                options.Add(new("FedEx 2Day (Except Intra-Hawaii)", "FEDEX_2_DAY"));
                options.Add(new("FedEx 2Day AM (Hawaii outbound only)", "FEDEX_2_DAY_AM"));
                options.Add(new("FedEx Express Saver® (Except Alaska and Hawaii)", "FEDEX_EXPRESS_SAVER"));
                options.Add(new("FedEx SameDay", "SAME_DAY"));
                options.Add(new("FedEx SameDay City (Selected U.S. Metro Areas)", "SAME_DAY_CITY"));

                break;

            case "Pickup Type":
                options.Add(new("Contact FedEx to schedule", "CONTACT_FEDEX_TO_SCHEDULE"));
                options.Add(new("Dropp off at FedEx location", "DROPOFF_AT_FEDEX_LOCATION"));
                options.Add(new("Use scheduled pickup", "USE_SCHEDULED_PICKUP"));

                break;

            case "Packing Type":
                options.Add(new("Customer packaging", "YOUR_PACKAGING"));
                options.Add(new("FedEx Envelope (Макс: 1 lbs/0.5 KG)", "FEDEX_ENVELOPE"));
                options.Add(new("FedEx Box (Макс: 20 lbs/9 KG)", "FEDEX_BOX"));
                options.Add(new("FedEx Small Box (Макс: 20 lbs/9 KG)", "FEDEX_SMALL_BOX"));
                options.Add(new("FedEx Medium Box (Макс: 20 lbs/9 KG)", "FEDEX_MEDIUM_BOX"));
                options.Add(new("FedEx Large Box (Макс: 20 lbs/9 KG)", "FEDEX_LARGE_BOX"));
                options.Add(new("FedEx Extra Large Box (Макс: 20 lbs/9 KG)", "FEDEX_EXTRA_LARGE_BOX"));
                options.Add(new("FedEx 10kg Box (Макс: 22 lbs/10 KG)", "FEDEX_10KG_BOX"));
                options.Add(new("FedEx 25kg Box (Макс: 55 lbs/25 KG)", "FEDEX_25KG_BOX"));
                options.Add(new("FedEx Pak (Макс: 20 lbs/9 KG)", "FEDEX_PAK"));
                options.Add(new("FedEx Tube (Макс: 20 lbs/9 KG)", "FEDEX_TUBE"));

                break;

            case "Origination Country":
                foreach (Country country in Services.Countries.GetCountries())                
                    options.Add(new(country.GetName(languageId), country.Code2));                

                break;
        }

        return options;
    }

    public IEnumerable<ParameterOption> GetParameterOptions(string parameterName, Func<string, object?> parameterValueLookup)
    {
        string languageId = Services.Languages.GetDefaultLanguageId();

        var options = new List<ParameterOption>();

        switch (parameterName)
        {
            case "Origination State/Region":
                string countryCode = Converter.ToString(parameterValueLookup("Origination Country"));

                foreach (Country region in Services.Countries.GetRegions(countryCode))
                    options.Add(new(region.GetName(languageId), region.RegionCode));

                break;
        }

        return options;
    }

}
