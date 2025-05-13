using Dynamicweb.Core.Helpers;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models;
using Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.Rates;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Service;

internal static class RateRequestCreationHelper
{
    public static RateAndTransitTimesRequest CreateRateAndTransitTimesRequest(Order order, FedEx provider)
    {
        var request = new RateAndTransitTimesRequest();
        request.AccountNumber.Value = provider.AccountNumber;

        request.RateRequestControlParameters = new()
        {
            ReturnTransitTimes = true
        };

        request.CarrierCodes = ["FDXE", "FDXG"];
        request.RequestedShipment = new()
        {
            RateRequestType = ["ACCOUNT", "LIST"],
            ShipDateStamp = DateTime.Now.ToString(DateHelper.DateOnlyFormatStringSortable),
            PickupType = provider.PickupType,
            ServiceType = provider.ServiceType,
            PackagingType = provider.PackingType,
            PreferredCurrency = order.PriceBeforeFees.Currency.Code,
            TotalPackageCount = order.ProductOrderLines.Count,
            Shipper = new()
            {
                Address = GetOriginAddress(order, provider)
            },
            Recipient = new()
            {
                Address = GetDestinationAddress(order, provider)
            },
            RequestedPackageLineItems = GetPackageLineItems(order, provider.UseLbInsteadOfKg).ToArray(),
            TotalWeight = order.Weight
        };

        return request;
    }

    private static IEnumerable<RequestedPackageLineItem> GetPackageLineItems(Order order, bool useLbInsteadOfKg)
    {
        foreach (OrderLine orderLine in order.ProductOrderLines)
        {
            var packageLineItem = new RequestedPackageLineItem
            {
                GroupPackageCount = 1
            };

            packageLineItem.Weight = new()
            {
                Units = useLbInsteadOfKg ? "LB" : "KG",
                Value = orderLine.Weight
            };

            packageLineItem.DeclaredValue = new()
            {
                Amount = orderLine.Price.Price,
                Currency = orderLine.Price.Currency.Code
            };

            yield return packageLineItem;
        }
    }

    private static AddressDetails GetOriginAddress(Order order, FedEx provider)
    {
        var address = new AddressDetails
        {
            CountryCode = provider.ShipperCountryCode,
            City = provider.ShipperCity,
            StreetLines = GetStreetLines(provider.ShipperStreet, provider.ShipperStreet2),
            PostalCode = provider.ShipperPostalCode
        };

        address.StateOrProvinceCode = provider.ShipperCountryCode.Equals("US", StringComparison.Ordinal)
            ? provider.ShipperStateOrProvinceCode1
            : provider.ShipperStateOrProvinceCode2;

        return address;
    }

    private static AddressDetails GetDestinationAddress(Order order, FedEx provider)
    {
        bool isDeliveryFieldsFilled = !string.IsNullOrEmpty(order.DeliveryAddress) && !string.IsNullOrEmpty(order.DeliveryAddress2);

        if (isDeliveryFieldsFilled)
        {
            return new()
            {
                CountryCode = order.DeliveryCountryCode,
                StateOrProvinceCode = order.DeliveryRegion,
                City = order.DeliveryCity,
                StreetLines = GetStreetLines(order.DeliveryAddress, order.DeliveryAddress2),
                PostalCode = order.DeliveryZip,
                Residential = provider.UseResidentialRates
            };
        }

        return new()
        {
            CountryCode = order.CustomerCountryCode,
            StateOrProvinceCode = order.CustomerRegion,
            City = order.CustomerCity,
            StreetLines = GetStreetLines(order.CustomerAddress, order.CustomerAddress2),
            PostalCode = order.CustomerZip,
            Residential = provider.UseResidentialRates
        };
    }

    private static IEnumerable<string> GetStreetLines(string address1, string address2)
    {
        if (!string.IsNullOrWhiteSpace(address1))        
            yield return address1;
        
        if (!string.IsNullOrWhiteSpace(address2))        
            yield return address2;        
    }   
}
