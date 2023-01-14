using Dynamicweb.Core;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.Orders.AddressValidation;
using Dynamicweb.Ecommerce.ShippingProviders.FedEx.FedexAddressValidationService;
using Dynamicweb.Extensibility.AddIns;
using Dynamicweb.Extensibility.Editors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx
{
    /// <summary>
    /// FedEx address validation provider
    /// </summary>
    [AddInName("FedEx address validation provider")]
    public class FedexAddressValidationProvider : AddressValidatorProvider
    {
        #region "Parameters"

        [AddInParameter("ServiceURL"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
        public string ServiceURL { get; set; }

        [AddInParameter("Key"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
        public string Key { get; set; }

        [AddInParameter("Password"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
        public string Password { get; set; }

        [AddInParameter("Account Number"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
        public string AccountNumber { get; set; }

        [AddInParameter("Meter Number"), AddInParameterEditor(typeof(TextParameterEditor), "size=80")]
        public string MeterNumber { get; set; }

        [AddInParameter("Validate Billing Address"), AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        public bool ValidateBillingAddress { get; set; }

        [AddInParameter("Validate Shipping Address"), AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        public bool ValidateShippingAddress { get; set; }

        [AddInParameter("Debug"), AddInParameterEditor(typeof(YesNoParameterEditor), ""), AddInDescription("Create a log of the request and response from FedEx")]
        public bool Debug { get; set; }

        #endregion

        /// <summary>
        /// Defaukt constructor
        /// </summary>
        public FedexAddressValidationProvider()
        {
            if (string.IsNullOrWhiteSpace(ServiceURL))
            {
                ServiceURL = "https://ws.fedex.com:443/web-services/addressvalidation";
            }
        }

        /// <summary>
        /// Validate order billing and delivery addresses
        /// </summary>
        /// <param name="order">Order for validation</param>
        public override void Validate(Order order)
        {
            if (ValidateBillingAddress)
            {
                DoValidate(order, GetBillingAddress(order), AddressType.Billing);
            }

            if (ValidateShippingAddress)
            {
                DoValidate(order, GetDeliveryAddress(order), AddressType.Delivery);
            }
        }

        private void DoValidate(Order order, AddressToValidate addressToValidate, AddressType addressType)
        {
            var addressValidatorResult = new AddressValidatorResult(ValidatorId, addressType);

            try
            {
                if (string.IsNullOrEmpty(addressToValidate.Address.PostalCode) || addressToValidate.Address.StreetLines.Length == 0)
                {
                    addressValidatorResult.IsError = true;
                    addressValidatorResult.ErrorMessage = "Insufficient address information";
                    order.AddressValidatorResults.Add(addressValidatorResult);

                    return;
                }

                var addressValidationReply = GetValidationReplyFromCache(addressToValidate, addressType);

                if (addressValidationReply == null)
                {
                    var service = new AddressValidationPortTypeClient();
                    service.Endpoint.Address = new System.ServiceModel.EndpointAddress(ServiceURL);

                    var request = CreateAddressValidationRequest(addressToValidate);
                    addressValidationReply = service.addressValidation(request);

                    StoreValidationReply(addressToValidate, addressType, addressValidationReply);
                }

                if (addressValidationReply.HighestSeverity == NotificationSeverityType.SUCCESS || addressValidationReply.HighestSeverity == NotificationSeverityType.NOTE || addressValidationReply.HighestSeverity == NotificationSeverityType.WARNING)
                {
                    ProccessAddressValidationReply(addressToValidate, addressValidatorResult, addressValidationReply);
                }
                else
                {
                    addressValidatorResult.IsError = true;
                    addressValidatorResult.ErrorMessage = GetNotificationsMessage(addressValidationReply);
                }

                if (Debug)
                {
                    SaveReply(addressValidationReply);
                }
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

        private void ProccessAddressValidationReply(AddressToValidate addressToValidate, AddressValidatorResult addressValidatorResult, AddressValidationReply reply)
        {
            var addrResult = reply.AddressResults[0];

            if (addrResult.Attributes == null)
            {
                addressValidatorResult.IsError = true;
                addressValidatorResult.ErrorMessage = "Invalid address";
                return;
            }

            if (!Converter.ToBoolean(GetAttribute(addrResult, "CountrySupported")))
            {
                addressValidatorResult.IsError = true;
                addressValidatorResult.ErrorMessage = "Country not supported";
                return;
            }

            var resolvedAttribute = GetAttribute(addrResult, "Resolved");
            if (resolvedAttribute != null)
            {
                if (!Converter.ToBoolean(resolvedAttribute))
                {
                    addressValidatorResult.IsError = true;
                    addressValidatorResult.ErrorMessage = "Unable to resolve an address";
                }
            }

            var validAddress = addrResult.EffectiveAddress;

            string oldLine1 = string.Empty;
            if (addressToValidate.Address.StreetLines.Length > 0)
                oldLine1 = addressToValidate.Address.StreetLines[0];

            string validLine1 = string.Empty;
            if (validAddress.StreetLines.Length > 0)
            {
                validLine1 = validAddress.StreetLines[0];
            }

            addressValidatorResult.CheckAddressField(AddressFieldType.AddressLine1, oldLine1, validLine1);
            addressValidatorResult.CheckAddressField(AddressFieldType.City, addressToValidate.Address.City, validAddress.City);
            addressValidatorResult.CheckAddressField(AddressFieldType.Region, addressToValidate.Address.StateOrProvinceCode, validAddress.StateOrProvinceCode);
            addressValidatorResult.CheckAddressField(AddressFieldType.ZipCode, addressToValidate.Address.PostalCode, validAddress.PostalCode);
        }

        private string GetAttribute(AddressValidationResult validationResult, string attributeName)
        {
            if (validationResult.Attributes == null)
            {
                return null;
            }

            foreach (AddressAttribute attribute in validationResult.Attributes)
            {
                if (attribute.Name == attributeName)
                {
                    return attribute.Value;
                }
            }

            return null;
        }

        private string GetNotificationsMessage(AddressValidationReply reply)
        {
            var stringBuilder = new StringBuilder();

            foreach (var notification in reply.Notifications)
            {
                stringBuilder.AppendLine(notification.Message);
            }

            return stringBuilder.ToString();
        }

        private AddressToValidate GetDeliveryAddress(Order order)
        {
            var address = new AddressToValidate();
            address.ClientReferenceId = "REF_DELIVERY";
            address.Address = new Address();
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
            address.Address = new Address();
            address.Address.StreetLines = GetStreetLines(order.CustomerAddress, order.CustomerAddress2);
            address.Address.City = order.CustomerCity;
            address.Address.PostalCode = order.CustomerZip;
            address.Address.CountryCode = order.CustomerCountryCode;
            address.Address.StateOrProvinceCode = order.CustomerRegion;

            return address;
        }

        private string[] GetStreetLines(params string[] addressStrings)
        {
            var result = new List<string>(addressStrings.Length);

            foreach (var address in addressStrings)
            {
                if (!string.IsNullOrWhiteSpace(address))
                {
                    result.Add(address);
                }
            }

            return result.ToArray();
        }

        private AddressValidationRequest CreateAddressValidationRequest(AddressToValidate addressToValidate)
        {
            var request = new AddressValidationRequest();
            request.WebAuthenticationDetail = new WebAuthenticationDetail();
            request.WebAuthenticationDetail.UserCredential = new WebAuthenticationCredential();
            request.WebAuthenticationDetail.UserCredential.Key = Key;
            request.WebAuthenticationDetail.UserCredential.Password = Password;

            request.ClientDetail = new ClientDetail();
            request.ClientDetail.AccountNumber = AccountNumber;
            request.ClientDetail.MeterNumber = MeterNumber;

            request.TransactionDetail = new TransactionDetail();
            request.TransactionDetail.CustomerTransactionId = "***Address Validation Request using VC#***";

            request.Version = new VersionId();

            request.InEffectAsOfTimestamp = DateTime.Now;
            request.InEffectAsOfTimestampSpecified = true;

            request.AddressesToValidate = new AddressToValidate[1];
            request.AddressesToValidate[0] = addressToValidate;

            return request;
        }

        private void SaveReply(AddressValidationReply reply)
        {
            var stringBuilder = new StringBuilder();

            if (reply.HighestSeverity == NotificationSeverityType.SUCCESS ||
                reply.HighestSeverity == NotificationSeverityType.NOTE ||
                reply.HighestSeverity == NotificationSeverityType.WARNING)
            {
                SaveAddressValidationReply(stringBuilder, reply);
            }

            stringBuilder.Append(GetNotificationsMessage(reply));

            SaveLog(stringBuilder.ToString());
        }

        private void SaveAddressValidationReply(StringBuilder stringBuilder, AddressValidationReply reply)
        {
            stringBuilder.AppendLine("AddressValidationReply details:");
            stringBuilder.AppendLine("*****************************************************");

            foreach (var result in reply.AddressResults)
            {
                stringBuilder.AppendLine("Address Id : " + result.ClientReferenceId);
                if (result.ClassificationSpecified) { Console.WriteLine("Classification: " + result.Classification); }
                if (result.StateSpecified) { Console.WriteLine("State: " + result.State); }
                stringBuilder.AppendLine("Proposed Address--");

                var address = result.EffectiveAddress;
                foreach (string street in address.StreetLines)
                {
                    stringBuilder.AppendLine("Street:  " + street);
                }

                stringBuilder.AppendLine("City:                " + address.City);
                stringBuilder.AppendLine("StateOrProvinceCode: " + address.StateOrProvinceCode);
                stringBuilder.AppendLine("PostalCode:          " + address.PostalCode);
                stringBuilder.AppendLine("CountryCode:         " + address.CountryCode);
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("Address Attributes:");

                foreach (var attribute in result.Attributes)
                {
                    stringBuilder.AppendLine(string.Format("  {0}: {1}", attribute.Name, attribute.Value));
                }
            }
        }

        #region Cache address validator request

        private string CacheKey(int validatorId, AddressType addressType)
        {
            return string.Format("AddressServiceRequest_{0}_{1}", validatorId, addressType);
        }

        private AddressValidationReply GetValidationReplyFromCache(AddressToValidate address, AddressType addressType)
        {
            if (Context.Current.Session[CacheKey(ValidatorId, addressType)] == null)
            {
                return null;
            }

            var cachedRequest = Context.Current.Session[CacheKey(ValidatorId, addressType)] as ValidateCache;
            if (cachedRequest == null)
            {
                return null;
            }

            if (address.Address.CountryCode != cachedRequest.Address.Address.CountryCode ||
                address.Address.CountryName != cachedRequest.Address.Address.CountryName ||
                address.Address.PostalCode != cachedRequest.Address.Address.PostalCode ||
                address.Address.City != cachedRequest.Address.Address.City ||
                address.Address.StateOrProvinceCode != cachedRequest.Address.Address.StateOrProvinceCode)
            {
                return null;
            }

            if (address.Address.StreetLines.Length != cachedRequest.Address.Address.StreetLines.Length)
            {
                return null;
            }

            foreach (var line in address.Address.StreetLines)
            {
                if (!cachedRequest.Address.Address.StreetLines.Contains(line))
                {
                    return null;
                }
            }

            return cachedRequest.ValidateResult;
        }

        private void StoreValidationReply(AddressToValidate address, AddressType addressType, AddressValidationReply validateResult)
        {
            Context.Current.Session[CacheKey(ValidatorId, addressType)] = new ValidateCache
            {
                Address = address,
                ValidateResult = validateResult
            };
        }

        private class ValidateCache
        {
            public AddressToValidate Address;
            public AddressValidationReply ValidateResult;
        }

        #endregion
    }
}
