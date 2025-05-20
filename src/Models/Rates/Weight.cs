using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.Rates;

[DataContract]
internal class Weight
{
    [DataMember(Name = "units", IsRequired = true)]
    public string Units { get; set; } = "";

    [DataMember(Name = "value", IsRequired = true)]
    public double Value { get; set; }
}
