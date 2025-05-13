using Dynamicweb.Core;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.Orders.AddressValidation;
using Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.AddressValidation;
using Dynamicweb.Ecommerce.ShippingProviders.FedEx.Service;
using Dynamicweb.Extensibility.AddIns;
using Dynamicweb.Extensibility.Editors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx;

/// <summary>
/// FedEx address validation provider
/// </summary>
[AddInName("FedEx address validation provider")]
public class FedexAddressValidationProvider : AddressValidatorProvider
{
    #region "Parameters"

    [AddInParameter("Api key"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
    public string ApiKey { get; set; } = "";

    [AddInParameter("Secret key"), AddInParameterEditor(typeof(TextParameterEditor), "size=80; password=true")]
    public string SecretKey { get; set; } = "";

    [AddInParameter("Account Number"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
    public string AccountNumber { get; set; } = "";

    [AddInParameter("Validate Billing Address"), AddInParameterEditor(typeof(YesNoParameterEditor), "")]
    public bool ValidateBillingAddress { get; set; }

    [AddInParameter("Validate Shipping Address"), AddInParameterEditor(typeof(YesNoParameterEditor), "")]
    public bool ValidateShippingAddress { get; set; }

    [AddInParameter("Debug"), AddInParameterEditor(typeof(YesNoParameterEditor), "infoText=Create a log of the request and response from FedEx")]
    public bool Debug { get; set; }

    [AddInParameter("Test Mode"), AddInParameterEditor(typeof(YesNoParameterEditor), "infoText=Set to use sandbox (test mode) for the API requests. Uncheck when ready for production.")]
    public bool TestMode { get; set; }

    #endregion

    /// <summary>
    /// Validate order billing and delivery addresses
    /// </summary>
    /// <param name="order">Order for validation</param>
    public override void Validate(Order order)
    {
        var service = new FedExService
        {
            ApiKey = ApiKey,
            SecretKey = SecretKey,
            DebugLog = Debug,
            TestMode = TestMode
        };

        if (ValidateBillingAddress)
            DoValidate(service, order, GetBillingAddress(order), AddressType.Billing);

        if (ValidateShippingAddress)
            DoValidate(service, order, GetDeliveryAddress(order), AddressType.Delivery);
    }

    private static string[] GetStreetLines(params string[] addressStrings) => addressStrings
       .Where(address => !string.IsNullOrWhiteSpace(address))
       .ToArray();

    private void DoValidate(FedExService service, Order order, AddressToValidate addressToValidate, AddressType addressType)
    {
        var addressValidatorResult = new AddressValidatorResult(ValidatorId, addressType);

        try
        {
            if (string.IsNullOrEmpty(addressToValidate.Address.PostalCode) || !addressToValidate.Address.StreetLines.Any())
            {
                addressValidatorResult.IsError = true;
                addressValidatorResult.ErrorMessage = "Insufficient address information";
                order.AddressValidatorResults.Add(addressValidatorResult);

                return;
            }

            var addressValidationResponse = GetValidationReplyFromCache(addressToValidate, addressType);

            if (addressValidationResponse is null)
            {
                addressValidationResponse = service.ValidateAddress(addressToValidate, order.Id);
                if (addressValidationResponse is null)
                    throw new NullReferenceException("Address validation response has no data.");

                StoreValidationResponse(addressToValidate, addressType, addressValidationResponse);
            }

            ProccessAddressValidationResponse(addressToValidate, addressValidatorResult, addressValidationResponse);
        }
        catch (Exception ex)
        {
            addressValidatorResult.IsError = true;
            addressValidatorResult.ErrorMessage = string.Format("FedEx threw an exception while validating address: {0}", ex.Message);
        }

        if (addressValidatorResult.IsError || addressValidatorResult.AddressFields.Count > 0)
        {
            order.AddressValidatorResults.Add(addressValidatorResult);
        }
    }

    private void ProccessAddressValidationResponse(AddressToValidate addressToValidate, AddressValidatorResult addressValidatorResult, ValidateAddressResponse response)
    {
        var addrResult = response.Output?.ResolvedAddresses?.FirstOrDefault();

        if (addrResult?.Attributes is null)
        {
            addressValidatorResult.IsError = true;
            addressValidatorResult.ErrorMessage = "Invalid address";

            return;
        }

        ValidateAddressAttributes attributes = addrResult.Attributes;

        if (!attributes.IsCountrySupported())
        {
            addressValidatorResult.IsError = true;
            addressValidatorResult.ErrorMessage = "Country not supported";

            return;
        }

        if (!attributes.IsResolved())
        {
            addressValidatorResult.IsError = true;
            addressValidatorResult.ErrorMessage = "Unable to resolve an address";

            return;
        }

        string? oldLine1 = string.Empty;
        if (addressToValidate.Address.StreetLines.Any())
            oldLine1 = addressToValidate.Address.StreetLines.FirstOrDefault();

        string? validLine1 = string.Empty;
        if (addrResult.StreetLinesToken?.Any() is true)
            validLine1 = addrResult.StreetLinesToken?.FirstOrDefault();

        addressValidatorResult.CheckAddressField(AddressFieldType.AddressLine1, oldLine1, validLine1);
        addressValidatorResult.CheckAddressField(AddressFieldType.City, addressToValidate.Address.City ?? "", addrResult.СityToken?.FirstOrDefault()?.Value ?? "");
        addressValidatorResult.CheckAddressField(AddressFieldType.Region, addressToValidate.Address.StateOrProvinceCode ?? "", addrResult.StateOrProvinceCode ?? "");
        addressValidatorResult.CheckAddressField(AddressFieldType.ZipCode, addressToValidate.Address.PostalCode ?? "", addrResult.PostalCodeToken?.Value ?? "");
    }

    private AddressToValidate GetDeliveryAddress(Order order)
    {
        var address = new AddressToValidate();
        address.ClientReferenceId = "REF_DELIVERY";
        address.Address.StreetLines = GetStreetLines(order.DeliveryAddress, order.DeliveryAddress2);
        address.Address.City = order.DeliveryCity;
        address.Address.PostalCode = order.DeliveryZip;
        address.Address.CountryCode = order.DeliveryCountryCode;
        address.Address.StateOrProvinceCode = order.DeliveryRegion;

        return address;
    }

    private AddressToValidate GetBillingAddress(Order order)
    {
        var address = new AddressToValidate();
        address.ClientReferenceId = "REF_CUSTOMER";
        address.Address.StreetLines = GetStreetLines(order.CustomerAddress, order.CustomerAddress2);
        address.Address.City = order.CustomerCity;
        address.Address.PostalCode = order.CustomerZip;
        address.Address.CountryCode = order.CustomerCountryCode;
        address.Address.StateOrProvinceCode = order.CustomerRegion;

        return address;
    }

    #region Cache address validator request

    private string CacheKey(int validatorId, AddressType addressType)
        => string.Format("AddressServiceRequest_{0}_{1}", validatorId, addressType);

    private ValidateAddressResponse? GetValidationReplyFromCache(AddressToValidate address, AddressType addressType)
    {
        if (Context.Current?.Session is null)
            return null;

        if (Context.Current.Session[CacheKey(ValidatorId, addressType)] is null)
            return null;

        var cachedRequest = Context.Current.Session[CacheKey(ValidatorId, addressType)] as ValidateCache;
        if (cachedRequest is null)
            return null;

        if (!string.Equals(address.Address.CountryCode, cachedRequest.Address?.Address?.CountryCode, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(address.Address.CountryCode, cachedRequest.Address?.Address?.CountryCode, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(address.Address.PostalCode, cachedRequest.Address?.Address?.PostalCode, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(address.Address.City, cachedRequest.Address?.Address?.City, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(address.Address.StateOrProvinceCode, cachedRequest.Address?.Address?.StateOrProvinceCode, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (address.Address.StreetLines.Count() != cachedRequest.Address?.Address?.StreetLines?.Count())
            return null;

        foreach (var line in address.Address.StreetLines)
        {
            if (cachedRequest.Address?.Address?.StreetLines?.Contains(line) is false or null)
                return null;
        }

        return cachedRequest.ValidateResult;
    }

    private void StoreValidationResponse(AddressToValidate address, AddressType addressType, ValidateAddressResponse validateResult)
    {
        if (Context.Current?.Session is null)
            return;

        Context.Current.Session[CacheKey(ValidatorId, addressType)] = new ValidateCache
        {
            Address = address,
            ValidateResult = validateResult
        };
    }

    #endregion
}
