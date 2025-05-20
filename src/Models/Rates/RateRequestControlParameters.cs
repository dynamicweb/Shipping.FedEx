using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.Rates;

[DataContract]
internal sealed class RateRequestControlParameters
{
    [DataMember(Name = "returnTransitTimes", EmitDefaultValue = false)]
    public bool ReturnTransitTimes { get; set; }

    [DataMember(Name = "servicesNeededOnRateFailure", EmitDefaultValue = false)]
    public bool ServicesNeededOnRateFailure { get; set; }
}
