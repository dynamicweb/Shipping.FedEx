using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.Rates;

[DataContract]
internal sealed class RateAndTransitTimesRequest
{
    [DataMember(Name = "accountNumber", IsRequired = true)]
    public AccountNumber AccountNumber { get; set; } = new();

    [DataMember(Name = "rateRequestControlParameters", EmitDefaultValue = false)]
    public RateRequestControlParameters? RateRequestControlParameters { get; set; }

    [DataMember(Name = "requestedShipment", IsRequired = true)]
    public RequestedShipment RequestedShipment { get; set; } = new();

    [DataMember(Name = "carrierCodes", EmitDefaultValue = false)]
    public IEnumerable<string>? CarrierCodes { get; set; }
}
