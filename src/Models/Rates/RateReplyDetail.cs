using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.Rates;

[DataContract]
internal sealed class RateReplyDetail
{
    [DataMember(Name = "serviceType")]
    public string ServiceType { get; set; } = "";

    [DataMember(Name = "serviceName")]
    public string ServiceName { get; set; } = "";

    [DataMember(Name = "packagingType")]
    public string PackagingType { get; set; } = "";

    [DataMember(Name = "signatureOptionType")]
    public string SignatureOptionType { get; set; } = "";

    [DataMember(Name = "customerMessages")]
    public IEnumerable<CustomerMessage>? CustomerMessages { get; set; }

    [DataMember(Name = "ratedShipmentDetails")]
    public IEnumerable<RatedShipmentDetail>? RatedShipmentDetails { get; set; }
}
