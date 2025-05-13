using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models;

[DataContract]
internal sealed class ParameterData
{
    [DataMember(Name = "value")]
    public string Value { get; set; } = "";

    [DataMember(Name = "key")]
    public string Key { get; set; } = "";
}
