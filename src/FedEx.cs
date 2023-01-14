using Dynamicweb.Ecommerce.Cart;
using Dynamicweb.Ecommerce.International;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Ecommerce.ShippingProviders.FedEx.RateServiceWebReference;
using Dynamicweb.Extensibility.AddIns;
using Dynamicweb.Extensibility.Editors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FedexMoney = Dynamicweb.Ecommerce.ShippingProviders.FedEx.RateServiceWebReference.Money;
using FedexRateReply = Dynamicweb.Ecommerce.ShippingProviders.FedEx.RateServiceWebReference.RateReply;
using FedexRateRequest = Dynamicweb.Ecommerce.ShippingProviders.FedEx.RateServiceWebReference.RateRequest;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx
{
    /// <summary>
    /// FedEx Shipping Service
    /// </summary>
    [AddInName("FedEx (Beta)"), AddInDescription("FedEx Shipping Provider")]
    public class FedEx : ShippingProvider, IDropDownOptions
    {
        #region "Parameters"

        [AddInParameter("ServiceURL"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
        public string ServiceURL { get; set; }

        [AddInParameter("Key"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
        public string Key { get; set; }

        [AddInParameter("Password"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
        public string Password { get; set; }

        [AddInParameter("Account Number"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
        /// <summary>
        /// This is a Summery 
        /// </summary>
		public string AccountNumber { get; set; }

        [AddInParameter("Meter Number"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
        public string MeterNumber { get; set; }

        [AddInParameter("Service Type"), AddInParameterEditor(typeof(DropDownParameterEditor), "SortBy=Value"),
            AddInDescription("Identifies the FedEx service to use in shipping the package for a rate request.")]
        public string ServiceType { get; set; }

        [AddInParameter("Dropoff Type"), AddInParameterEditor(typeof(DropDownParameterEditor), "SortBy=Value"),
            AddInDescription("Identifies the method by which the package is to be tendered to FedEx. This element does not dispatch a courier for package pickup.")]
        public string DropoffType { get; set; }

        [AddInParameter("Packing Type"), AddInParameterEditor(typeof(DropDownParameterEditor), "SortBy=Value"),
            AddInDescription("Identifies the packaging used by the requester for the package.")]
        public string PackingType { get; set; }

        [AddInParameter("Origination Street Address"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
        public string ShipperStreet { get; set; }

        [AddInParameter("Origination Street Address 2"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
        public string ShipperStreet2 { get; set; }

        [AddInParameter("Origination City"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
        public string ShipperCity { get; set; }

        [AddInParameter("Origination State/Region"), AddInParameterEditor(typeof(DropDownParameterEditor), "SortBy=Value")]
        public string ShipperStateOrProvinceCode1 { get; set; }

        [AddInParameter("Origination State/Region 2"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
        public string ShipperStateOrProvinceCode2 { get; set; }

        [AddInParameter("Origination Zip Code"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
        public string ShipperPostalCode { get; set; }

        [AddInParameter("Origination Country"), AddInParameterEditor(typeof(DropDownParameterEditor), "SortBy=Value")]
        public string ShipperCountryCode { get; set; }

        [AddInParameter("Use Residential Rates"), AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        public bool UseResidentialRates { get; set; }

        [AddInParameter("Debug"), AddInParameterEditor(typeof(YesNoParameterEditor), ""), AddInDescription("Create a log of the request and response from FedEx")]
        public bool DebugLog { get; set; }

        [AddInParameter("Use LB instead og KG"), AddInParameterEditor(typeof(YesNoParameterEditor), ""), AddInDescription("Calculates shipping costs based on weights in LB")]
        public bool UseLbInsteadOfKg { get; set; }

        #endregion

        /// <summary>
        /// Default constructor
        /// </summary>
        public FedEx()
        {
            ServiceURL = "https://ws.fedex.com:443/web-services/rate";
        }

        /// <summary>
        /// Calculate shipping fee for the specified order
        /// </summary>
        /// <param name="Order">The order.</param>
        /// <returns>Returns shipping fee for the specified order</returns>
        public override PriceRaw CalculateShippingFee(Order order)
        {
            var requestCacheKey = order.AutoId.ToString();
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
                        if (rateRequest.Warning != null)
                        {
                            order.ShippingProviderWarnings.AddRange(rateRequest.Warning);
                        }
                        if (rateRequest.Errors != null)
                        {
                            order.ShippingProviderErrors.AddRange(rateRequest.Errors);
                        }
                    }
                    else
                    {
                        var service = new RatePortTypeClient();
                        service.Endpoint.Address = new System.ServiceModel.EndpointAddress(ServiceURL);
                        var request = CreateRateRequest(order);

                        if (DebugLog)
                        {
                            SaveLog(string.Format("Order ID: {0} - WSDL Request", order.Id), true);
                        }

                        var reply = service.getRates(request);

                        if (DebugLog)
                        {
                            SaveReply(reply);
                        }

                        rate = ProcessResponse(reply, order);
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

        private void SaveReply(FedexRateReply reply)
        {
            var stringBuilder = new StringBuilder();

            if (reply.HighestSeverity == NotificationSeverityType.SUCCESS ||
                reply.HighestSeverity == NotificationSeverityType.NOTE ||
                reply.HighestSeverity == NotificationSeverityType.WARNING)
            {
                ShowRateReply(stringBuilder, reply);
            }
            ShowNotifications(stringBuilder, reply);

            SaveLog(stringBuilder.ToString(), false);
        }

        private FedexRateRequest CreateRateRequest(Order order)
        {
            var request = new FedexRateRequest();

            request.WebAuthenticationDetail = new WebAuthenticationDetail();
            request.WebAuthenticationDetail.UserCredential = new WebAuthenticationCredential();
            request.WebAuthenticationDetail.UserCredential.Key = Key;
            request.WebAuthenticationDetail.UserCredential.Password = Password;

            request.ClientDetail = new ClientDetail();
            request.ClientDetail.AccountNumber = AccountNumber;
            request.ClientDetail.MeterNumber = MeterNumber;

            request.TransactionDetail = new TransactionDetail();
            // This is a reference field for the customer. Any value can be used and will be provided in the response.
            request.TransactionDetail.CustomerTransactionId = "***Rate Available Services v16 Request - Dynamicweb***";

            request.Version = new VersionId();

            request.ReturnTransitAndCommit = true;
            request.ReturnTransitAndCommitSpecified = true;

            // Insert the Carriers you would like to see the rates for
            request.CarrierCodes = new CarrierCodeType[2];
            request.CarrierCodes[0] = CarrierCodeType.FDXE;
            request.CarrierCodes[1] = CarrierCodeType.FDXG;

            SetShipmentDetails(order, request);

            return request;
        }

        private void SetShipmentDetails(Dynamicweb.Ecommerce.Orders.Order order, FedexRateRequest request)
        {
            request.RequestedShipment = new RequestedShipment();
            request.RequestedShipment.ShipTimestamp = DateTime.Now;
            request.RequestedShipment.ShipTimestampSpecified = true;

            request.RequestedShipment.DropoffType = GetDropoffType(); //Drop off types are BUSINESS_SERVICE_CENTER, DROP_BOX, REGULAR_PICKUP, REQUEST_COURIER, STATION
            request.RequestedShipment.DropoffTypeSpecified = true;

            SetServiceType(request.RequestedShipment); // Service types are STANDARD_OVERNIGHT, PRIORITY_OVERNIGHT, FEDEX_GROUND ...
            SetPackagingType(request.RequestedShipment); // Packaging type FEDEX_BOK, FEDEX_PAK, FEDEX_TUBE, YOUR_PACKAGING, ...

            SetOrigin(request, order);

            SetDestination(request, order);

            SetPackageLineItems(request, order, UseLbInsteadOfKg);

            request.RequestedShipment.TotalInsuredValue = new FedexMoney();
            request.RequestedShipment.TotalInsuredValue.Amount = (decimal)order.PriceBeforeFees.Price;
            request.RequestedShipment.TotalInsuredValue.Currency = order.PriceBeforeFees.Currency.Code;

            request.RequestedShipment.PackageCount = order.ProductOrderLines.Count.ToString();
        }

        private static void SetPackageLineItems(FedexRateRequest request, Order order, bool useLbInsteadOfKg)
        {
            request.RequestedShipment.RequestedPackageLineItems = new RequestedPackageLineItem[order.ProductOrderLines.Count];

            for (int i = 0; i < order.ProductOrderLines.Count; i++)
            {
                var packageLineItem = new RequestedPackageLineItem();
                var orderLine = order.ProductOrderLines.ElementAt(i);

                packageLineItem.SequenceNumber = (i + 1).ToString();
                packageLineItem.GroupPackageCount = "1";

                packageLineItem.Weight = new Weight();
                if (useLbInsteadOfKg)
                {
                    packageLineItem.Weight.Units = WeightUnits.LB;
                }
                else
                {
                    packageLineItem.Weight.Units = WeightUnits.KG;
                }

                packageLineItem.Weight.UnitsSpecified = true;
                packageLineItem.Weight.Value = (decimal)orderLine.Weight;
                packageLineItem.Weight.ValueSpecified = true;

                packageLineItem.InsuredValue = new FedexMoney();
                packageLineItem.InsuredValue.Amount = (decimal)orderLine.Price.Price;
                packageLineItem.InsuredValue.Currency = orderLine.Price.Currency.Code;

                request.RequestedShipment.RequestedPackageLineItems[i] = packageLineItem;
            }
        }

        private void SetOrigin(FedexRateRequest request, Order order)
        {
            request.RequestedShipment.Shipper = new Party();
            request.RequestedShipment.Shipper.Address = new Address();
            request.RequestedShipment.Shipper.Address.StreetLines = GetAddress(ShipperStreet, ShipperStreet2);
            request.RequestedShipment.Shipper.Address.City = ShipperCity;
            request.RequestedShipment.Shipper.Address.PostalCode = ShipperPostalCode;
            request.RequestedShipment.Shipper.Address.CountryCode = ShipperCountryCode;

            if (ShipperCountryCode == "US")
            {
                request.RequestedShipment.Shipper.Address.StateOrProvinceCode = ShipperStateOrProvinceCode1;
            }
            else
            {
                request.RequestedShipment.Shipper.Address.StateOrProvinceCode = ShipperStateOrProvinceCode2;
            }
        }

        private void SetDestination(FedexRateRequest request, Order order)
        {
            request.RequestedShipment.Recipient = new Party();
            request.RequestedShipment.Recipient.Address = new Address();

            if (UseResidentialRates)
            {
                request.RequestedShipment.Recipient.Address.Residential = true;
                request.RequestedShipment.Recipient.Address.ResidentialSpecified = true;
            }

            bool isDeliveryFieldsFilled = !(string.IsNullOrEmpty(order.DeliveryAddress) && string.IsNullOrEmpty(order.DeliveryAddress2));
            if (isDeliveryFieldsFilled)
            {
                request.RequestedShipment.Recipient.Address.StreetLines = GetAddress(order.DeliveryAddress, order.DeliveryAddress2);
                request.RequestedShipment.Recipient.Address.City = order.DeliveryCity;
                request.RequestedShipment.Recipient.Address.PostalCode = order.DeliveryZip;
                request.RequestedShipment.Recipient.Address.StateOrProvinceCode = order.DeliveryRegion;
                request.RequestedShipment.Recipient.Address.CountryCode = order.DeliveryCountryCode;
            }
            else
            {
                request.RequestedShipment.Recipient.Address.StreetLines = GetAddress(order.CustomerAddress, order.CustomerAddress2);
                request.RequestedShipment.Recipient.Address.City = order.CustomerCity;
                request.RequestedShipment.Recipient.Address.PostalCode = order.CustomerZip;
                request.RequestedShipment.Recipient.Address.StateOrProvinceCode = order.CustomerRegion;
                request.RequestedShipment.Recipient.Address.CountryCode = order.CustomerCountryCode;
            }
        }

        private string[] GetAddress(params string[] addrs)
        {
            List<string> ret = new List<string>(addrs.Length);

            foreach (var addr in addrs)
            {
                if (!string.IsNullOrEmpty(ShipperStreet))
                    ret.Add(addr);
            }

            return ret.ToArray();
        }

        private DropoffType GetDropoffType()
        {
            DropoffType result;

            switch (DropoffType)
            {
                case "BUSINESS_SERVICE_CENTER":
                    result = RateServiceWebReference.DropoffType.BUSINESS_SERVICE_CENTER;
                    break;
                case "DROP_BOX":
                    result = RateServiceWebReference.DropoffType.DROP_BOX;
                    break;
                case "REGULAR_PICKUP":
                    result = RateServiceWebReference.DropoffType.REGULAR_PICKUP;
                    break;
                case "REQUEST_COURIER":
                    result = RateServiceWebReference.DropoffType.REQUEST_COURIER;
                    break;
                case "STATION":
                    result = RateServiceWebReference.DropoffType.STATION;
                    break;
                default:
                    result = RateServiceWebReference.DropoffType.BUSINESS_SERVICE_CENTER;
                    break;
            }

            return result;
        }

        private void SetPackagingType(RequestedShipment requestedShipment)
        {
            requestedShipment.PackagingTypeSpecified = true;

            switch (PackingType)
            {
                case "FEDEX_10KG_BOX":
                    requestedShipment.PackagingType = PackagingType.FEDEX_10KG_BOX;
                    break;
                case "FEDEX_25KG_BOX":
                    requestedShipment.PackagingType = PackagingType.FEDEX_25KG_BOX;
                    break;
                case "FEDEX_BOX":
                    requestedShipment.PackagingType = PackagingType.FEDEX_BOX;
                    break;
                case "FEDEX_ENVELOPE":
                    requestedShipment.PackagingType = PackagingType.FEDEX_ENVELOPE;
                    break;
                case "FEDEX_EXTRA_LARGE_BOX":
                    requestedShipment.PackagingType = PackagingType.FEDEX_EXTRA_LARGE_BOX;
                    break;
                case "FEDEX_LARGE_BOX":
                    requestedShipment.PackagingType = PackagingType.FEDEX_LARGE_BOX;
                    break;
                case "FEDEX_MEDIUM_BOX":
                    requestedShipment.PackagingType = PackagingType.FEDEX_MEDIUM_BOX;
                    break;
                case "FEDEX_PAK":
                    requestedShipment.PackagingType = PackagingType.FEDEX_PAK;
                    break;
                case "FEDEX_SMALL_BOX":
                    requestedShipment.PackagingType = PackagingType.FEDEX_SMALL_BOX;
                    break;
                case "FEDEX_TUBE":
                    requestedShipment.PackagingType = PackagingType.FEDEX_TUBE;
                    break;
                case "YOUR_PACKAGING":
                    requestedShipment.PackagingType = PackagingType.YOUR_PACKAGING;
                    break;
                default:
                    requestedShipment.PackagingTypeSpecified = false;
                    break;
            }
        }

        private void SetServiceType(RequestedShipment requestedShipment)
        {
            requestedShipment.ServiceTypeSpecified = true;

            switch (ServiceType)
            {
                case "EUROPE_FIRST_INTERNATIONAL_PRIORITY":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.EUROPE_FIRST_INTERNATIONAL_PRIORITY;
                    break;
                case "FEDEX_1_DAY_FREIGHT":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.FEDEX_1_DAY_FREIGHT;
                    break;
                case "FEDEX_2_DAY":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.FEDEX_2_DAY;
                    break;
                case "FEDEX_2_DAY_AM":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.FEDEX_2_DAY_AM;
                    break;
                case "FEDEX_2_DAY_FREIGHT":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.FEDEX_2_DAY_FREIGHT;
                    break;
                case "FEDEX_3_DAY_FREIGHT":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.FEDEX_3_DAY_FREIGHT;
                    break;
                case "FEDEX_DISTANCE_DEFERRED":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.FEDEX_DISTANCE_DEFERRED;
                    break;
                case "FEDEX_EXPRESS_SAVER":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.FEDEX_EXPRESS_SAVER;
                    break;
                case "FEDEX_FIRST_FREIGHT":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.FEDEX_FIRST_FREIGHT;
                    break;
                case "FEDEX_FREIGHT_ECONOMY":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.FEDEX_FREIGHT_ECONOMY;
                    break;
                case "FEDEX_FREIGHT_PRIORITY":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.FEDEX_FREIGHT_PRIORITY;
                    break;
                case "FEDEX_GROUND":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.FEDEX_GROUND;
                    break;
                case "FEDEX_NEXT_DAY_AFTERNOON":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.FEDEX_NEXT_DAY_AFTERNOON;
                    break;
                case "FEDEX_NEXT_DAY_EARLY_MORNING":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.FEDEX_NEXT_DAY_EARLY_MORNING;
                    break;
                case "FEDEX_NEXT_DAY_END_OF_DAY":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.FEDEX_NEXT_DAY_END_OF_DAY;
                    break;
                case "FEDEX_NEXT_DAY_FREIGHT":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.FEDEX_NEXT_DAY_FREIGHT;
                    break;
                case "FEDEX_NEXT_DAY_MID_MORNING":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.FEDEX_NEXT_DAY_MID_MORNING;
                    break;
                case "FIRST_OVERNIGHT":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.FIRST_OVERNIGHT;
                    break;
                case "GROUND_HOME_DELIVERY":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.GROUND_HOME_DELIVERY;
                    break;
                case "INTERNATIONAL_ECONOMY":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.INTERNATIONAL_ECONOMY;
                    break;
                case "INTERNATIONAL_ECONOMY_FREIGHT":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.INTERNATIONAL_ECONOMY_FREIGHT;
                    break;
                case "INTERNATIONAL_FIRST":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.INTERNATIONAL_FIRST;
                    break;
                case "INTERNATIONAL_PRIORITY":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.INTERNATIONAL_PRIORITY;
                    break;
                case "INTERNATIONAL_PRIORITY_FREIGHT":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.INTERNATIONAL_PRIORITY_FREIGHT;
                    break;
                case "PRIORITY_OVERNIGHT":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.PRIORITY_OVERNIGHT;
                    break;
                case "STANDARD_OVERNIGHT":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.STANDARD_OVERNIGHT;
                    break;
                case "SAME_DAY":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.SAME_DAY;
                    break;
                case "SAME_DAY_CITY":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.SAME_DAY_CITY;
                    break;
                case "SMART_POST":
                    requestedShipment.ServiceType = RateServiceWebReference.ServiceType.SMART_POST;
                    break;
                default:
                    requestedShipment.ServiceTypeSpecified = false;
                    break;
            }
        }

        private bool IsRequestParametersCorrect(Order order)
        {
            if (string.IsNullOrEmpty(order.DeliveryZip) && string.IsNullOrEmpty(order.CustomerZip))
            {
                order.ShippingProviderErrors.Add("ZipCode field is empty.");
            }

            if (string.IsNullOrEmpty(order.DeliveryAddress) &&
                string.IsNullOrEmpty(order.DeliveryAddress2) &&
                string.IsNullOrEmpty(order.CustomerAddress) &&
                string.IsNullOrEmpty(order.CustomerAddress2))
            {
                order.ShippingProviderErrors.Add("Delivery address is empty.");
            }

            return order.ShippingProviderErrors.Count == 0;
        }

        private double ProcessResponse(FedexRateReply reply, Order order)
        {
            if (reply.HighestSeverity == NotificationSeverityType.SUCCESS ||
                reply.HighestSeverity == NotificationSeverityType.NOTE ||
                reply.HighestSeverity == NotificationSeverityType.WARNING)
            {
                foreach (var rateReplyDetail in reply.RateReplyDetails)
                {
                    foreach (var shipmentDetail in rateReplyDetail.RatedShipmentDetails)
                    {
                        var rateDetail = shipmentDetail.ShipmentRateDetail;
                        if (rateDetail.TotalNetCharge != null)
                        {
                            return (double)rateDetail.TotalNetCharge.Amount;
                        }
                    }
                }
            }
            else
            {
                var stringBuilder = new StringBuilder();
                ShowNotifications(stringBuilder, reply);
                order.ShippingProviderWarnings.Add(stringBuilder.ToString());
            }

            return 0;
        }

        private void ShowNotifications(StringBuilder stringBuilder, FedexRateReply reply)
        {
            stringBuilder.AppendLine("Notifications");

            for (int i = 0; i < reply.Notifications.Length; i++)
            {
                var notification = reply.Notifications[i];
                stringBuilder.AppendFormat("Notification no. {0}\r\n", i);
                stringBuilder.AppendFormat(" Severity: {0}\r\n", notification.Severity);
                stringBuilder.AppendFormat(" Code: {0}\r\n", notification.Code);
                stringBuilder.AppendFormat(" Message: {0}\r\n", notification.Message);
                stringBuilder.AppendFormat(" Source: {0}\r\n", notification.Source);
            }
        }

        private void ShowRateReply(StringBuilder stringBuilder, FedexRateReply reply)
        {
            stringBuilder.AppendLine("RateReply details:");
            foreach (var rateReplyDetail in reply.RateReplyDetails)
            {
                if (rateReplyDetail.ServiceTypeSpecified)
                {
                    stringBuilder.AppendFormat("Service Type: {0}\r\n", rateReplyDetail.ServiceType);
                }

                if (rateReplyDetail.PackagingTypeSpecified)
                {
                    stringBuilder.AppendFormat("Packaging Type: {0}\r\n", rateReplyDetail.PackagingType);
                }

                stringBuilder.AppendLine();
                foreach (var shipmentDetail in rateReplyDetail.RatedShipmentDetails)
                {
                    ShowShipmentRateDetails(stringBuilder, shipmentDetail);
                    stringBuilder.AppendLine();
                }
                ShowDeliveryDetails(stringBuilder, rateReplyDetail);
                stringBuilder.AppendLine("**********************************************************");
            }
        }

        private void ShowDeliveryDetails(StringBuilder stringBuilder, RateReplyDetail rateDetail)
        {
            if (rateDetail.DeliveryTimestampSpecified)
            {
                stringBuilder.AppendLine("Delivery timestamp: " + rateDetail.DeliveryTimestamp.ToString());
            }
            if (rateDetail.TransitTimeSpecified)
            {
                stringBuilder.AppendLine("Transit time: " + rateDetail.TransitTime);
            }
        }

        private void ShowShipmentRateDetails(StringBuilder stringBuilder, RatedShipmentDetail shipmentDetail)
        {
            if (shipmentDetail == null) return;
            if (shipmentDetail.ShipmentRateDetail == null) return;

            var rateDetail = shipmentDetail.ShipmentRateDetail;
            stringBuilder.AppendLine("--- Shipment Rate Detail ---");
            stringBuilder.AppendFormat("RateType: {0} \r\n", rateDetail.RateType);

            if (rateDetail.TotalBillingWeight != null)
            {
                stringBuilder.AppendFormat(
                    "Total Billing Weight: {0} {1}\r\n",
                    rateDetail.TotalBillingWeight.Value, shipmentDetail.ShipmentRateDetail.TotalBillingWeight.Units
                );
            }

            if (rateDetail.TotalBaseCharge != null)
            {
                stringBuilder.AppendFormat(
                    "Total Base Charge: {0} {1}\r\n",
                    rateDetail.TotalBaseCharge.Amount, rateDetail.TotalBaseCharge.Currency
                );
            }

            if (rateDetail.TotalFreightDiscounts != null)
            {
                stringBuilder.AppendFormat(
                    "Total Freight Discounts: {0} {1}\r\n",
                    rateDetail.TotalFreightDiscounts.Amount, rateDetail.TotalFreightDiscounts.Currency
                );
            }

            if (rateDetail.TotalSurcharges != null)
            {
                stringBuilder.AppendFormat(
                    "Total Surcharges: {0} {1}\r\n",
                    rateDetail.TotalSurcharges.Amount, rateDetail.TotalSurcharges.Currency
                );
            }

            if (rateDetail.Surcharges != null)
            {
                // Individual surcharge for each package
                foreach (var surcharge in rateDetail.Surcharges)
                {
                    stringBuilder.AppendFormat(
                        " {0} surcharge {1} {2}\r\n",
                        surcharge.SurchargeType, surcharge.Amount.Amount, surcharge.Amount.Currency
                    );
                }
            }

            if (rateDetail.TotalNetCharge != null)
            {
                stringBuilder.AppendFormat(
                    "Total Net Charge: {0} {1}\r\n",
                    rateDetail.TotalNetCharge.Amount, rateDetail.TotalNetCharge.Currency
                );
            }
        }

        /// <summary> 
        /// Retrieves options 
        /// </summary>
        /// <param name="optionName">Service Type, Dropoff Type, Packing Type, Origination State/Region or Origination Country</param>
        /// <returns>Options with code and title</returns>
        public Hashtable GetOptions(string optionName)
        {
            var options = new Hashtable();

            switch (optionName)
            {
                case "Service Type":
                    options.Add("EUROPE_FIRST_INTERNATIONAL_PRIORITY", "EUROPE_FIRST_INTERNATIONAL_PRIORITY");
                    options.Add("FEDEX_1_DAY_FREIGHT", "FEDEX_1_DAY_FREIGHT");
                    options.Add("FEDEX_2_DAY", "FEDEX_2_DAY");
                    options.Add("FEDEX_2_DAY_AM", "FEDEX_2_DAY_AM");
                    options.Add("FEDEX_2_DAY_FREIGHT", "FEDEX_2_DAY_FREIGHT");
                    options.Add("FEDEX_3_DAY_FREIGHT", "FEDEX_3_DAY_FREIGHT");
                    options.Add("FEDEX_DISTANCE_DEFERRED", "FEDEX_DISTANCE_DEFERRED");
                    options.Add("FEDEX_EXPRESS_SAVER", "FEDEX_EXPRESS_SAVER");
                    options.Add("FEDEX_FIRST_FREIGHT", "FEDEX_FIRST_FREIGHT");
                    options.Add("FEDEX_FREIGHT_ECONOMY", "FEDEX_FREIGHT_ECONOMY");
                    options.Add("FEDEX_FREIGHT_PRIORITY", "FEDEX_FREIGHT_PRIORITY");
                    options.Add("FEDEX_GROUND", "FEDEX_GROUND");
                    options.Add("FEDEX_NEXT_DAY_AFTERNOON", "FEDEX_NEXT_DAY_AFTERNOON");
                    options.Add("FEDEX_NEXT_DAY_EARLY_MORNING", "FEDEX_NEXT_DAY_EARLY_MORNING");
                    options.Add("FEDEX_NEXT_DAY_END_OF_DAY", "FEDEX_NEXT_DAY_END_OF_DAY");
                    options.Add("FEDEX_NEXT_DAY_FREIGHT", "FEDEX_NEXT_DAY_FREIGHT");
                    options.Add("FEDEX_NEXT_DAY_MID_MORNING", "FEDEX_NEXT_DAY_MID_MORNING");
                    options.Add("FIRST_OVERNIGHT", "FIRST_OVERNIGHT");
                    options.Add("GROUND_HOME_DELIVERY", "GROUND_HOME_DELIVERY");
                    options.Add("INTERNATIONAL_ECONOMY", "INTERNATIONAL_ECONOMY");
                    options.Add("INTERNATIONAL_ECONOMY_FREIGHT", "INTERNATIONAL_ECONOMY_FREIGHT");
                    options.Add("INTERNATIONAL_FIRST", "INTERNATIONAL_FIRST");
                    options.Add("INTERNATIONAL_PRIORITY", "INTERNATIONAL_PRIORITY");
                    options.Add("INTERNATIONAL_PRIORITY_FREIGHT", "INTERNATIONAL_PRIORITY_FREIGHT");
                    options.Add("PRIORITY_OVERNIGHT", "PRIORITY_OVERNIGHT");
                    options.Add("STANDARD_OVERNIGHT", "STANDARD_OVERNIGHT");
                    options.Add("SAME_DAY", "SAME_DAY");
                    options.Add("SAME_DAY_CITY", "SAME_DAY_CITY");
                    options.Add("SMART_POST", "SMART_POST");
                    break;

                case "Dropoff Type":
                    options.Add("BUSINESS_SERVICE_CENTER", "BUSINESS_SERVICE_CENTER");
                    options.Add("DROP_BOX", "DROP_BOX");
                    options.Add("REGULAR_PICKUP", "REGULAR_PICKUP");
                    options.Add("REQUEST_COURIER", "REQUEST_COURIER");
                    options.Add("STATION", "STATION");
                    break;

                case "Packing Type":
                    options.Add("FEDEX_10KG_BOX", "FEDEX_10KG_BOX");
                    options.Add("FEDEX_25KG_BOX", "FEDEX_25KG_BOX");
                    options.Add("FEDEX_BOX", "FEDEX_BOX");
                    options.Add("FEDEX_ENVELOPE", "FEDEX_ENVELOPE");
                    options.Add("FEDEX_EXTRA_LARGE_BOX", "FEDEX_EXTRA_LARGE_BOX");
                    options.Add("FEDEX_LARGE_BOX", "FEDEX_LARGE_BOX");
                    options.Add("FEDEX_MEDIUM_BOX", "FEDEX_MEDIUM_BOX");
                    options.Add("FEDEX_PAK", "FEDEX_PAK");
                    options.Add("FEDEX_SMALL_BOX", "FEDEX_SMALL_BOX");
                    options.Add("FEDEX_TUBE", "FEDEX_TUBE");
                    options.Add("YOUR_PACKAGING", "YOUR_PACKAGING");
                    break;

                case "Origination State/Region":
                    options.Add("AL", "Alabama");
                    options.Add("AK", "Alaska");
                    options.Add("AZ", "Arizona");
                    options.Add("AR", "Arkansas");
                    options.Add("CA", "California");
                    options.Add("CO", "Colorado");
                    options.Add("CT", "Connecticut");
                    options.Add("DE", "Delaware");
                    options.Add("DC", "District of Columbia");
                    options.Add("FL", "Florida");
                    options.Add("GA", "Georgia");
                    options.Add("HI", "Hawaii");
                    options.Add("ID", "Idaho");
                    options.Add("IL", "Illinois");
                    options.Add("IN", "Indiana");
                    options.Add("IA", "Iowa");
                    options.Add("KS", "Kansas");
                    options.Add("KY", "Kentucky");
                    options.Add("LA", "Louisiana");
                    options.Add("ME", "Maine");
                    options.Add("MD", "Maryland");
                    options.Add("MA", "Massachusetts");
                    options.Add("MI", "Michigan");
                    options.Add("MN", "Minnesota");
                    options.Add("MS", "Mississippi");
                    options.Add("MO", "Missouri");
                    options.Add("MT", "Montana");
                    options.Add("NE", "Nebraska");
                    options.Add("NV", "Nevada");
                    options.Add("NH", "New Hampshire");
                    options.Add("NJ", "New Jersey");
                    options.Add("NM", "New Mexico");
                    options.Add("NY", "New York");
                    options.Add("NC", "North Carolina");
                    options.Add("ND", "North Dakota");
                    options.Add("OH", "Ohio");
                    options.Add("OK", "Oklahoma");
                    options.Add("OR", "Oregon");
                    options.Add("PA", "Pennsylvania");
                    options.Add("RI", "Rhode Island");
                    options.Add("SC", "South Carolina");
                    options.Add("SD", "South Dakota");
                    options.Add("TN", "Tennessee");
                    options.Add("TX", "Texas");
                    options.Add("UT", "Utah");
                    options.Add("VT", "Vermont");
                    options.Add("VA", "Virginia");
                    options.Add("WA", "Washington");
                    options.Add("WV", "West Virginia");
                    options.Add("WI", "Wisconsin");
                    options.Add("WY", "Wyoming");
                    break;

                case "Origination Country":
                    foreach (GlobalISO iso in GlobalISO.GetGlobalISOs())
                    {
                        if (!options.ContainsKey(iso.Code2))
                        {
                            options.Add(iso.Code2, iso.CountryUK);
                        }
                    }

                    break;
            }

            return options;
        }
    }
}
