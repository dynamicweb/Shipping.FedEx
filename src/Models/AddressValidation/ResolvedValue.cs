using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.AddressValidation;

[DataContract]
internal sealed class ResolvedValue
{
    [DataMember(Name = "changed")]
    public bool Changed { get; set; }

    [DataMember(Name = "value")]
    public string Value { get; set; } = "";
}