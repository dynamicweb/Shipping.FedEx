using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.Rates;

[DataContract]
internal sealed class RequestedShipment
{
    [DataMember(Name = "rateRequestType", IsRequired = true)]
    public IEnumerable<string> RateRequestType { get; set; } = [];

    [DataMember(Name = "shipper", IsRequired = true)]
    public LocationData Shipper { get; set; } = new();

    [DataMember(Name = "recipient", IsRequired = true)]
    public LocationData Recipient { get; set; } = new();

    [DataMember(Name = "serviceType", EmitDefaultValue = false)]
    public string? ServiceType { get; set; }

    [DataMember(Name = "preferredCurrency", EmitDefaultValue = false)]
    public string PreferredCurrency { get; set; } = "";

    [DataMember(Name = "shipDateStamp", EmitDefaultValue = false)]
    public string ShipDateStamp { get; set; } = "";

    [DataMember(Name = "pickupType", IsRequired = true)]
    public string PickupType { get; set; } = "";

    [DataMember(Name = "packagingType", EmitDefaultValue = false)]
    public string? PackagingType { get; set; }

    [DataMember(Name = "requestedPackageLineItems", IsRequired = true)]
    public IEnumerable<RequestedPackageLineItem> RequestedPackageLineItems { get; set; } = [];

    [DataMember(Name = "documentShipment", EmitDefaultValue = false)]
    public bool? DocumentShipment { get; set; }

    [DataMember(Name = "totalPackageCount", EmitDefaultValue = false)]
    public int? TotalPackageCount { get; set; }

    [DataMember(Name = "totalWeight", EmitDefaultValue = false)]
    public double? TotalWeight { get; set; }
}

