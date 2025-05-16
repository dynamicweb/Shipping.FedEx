using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.Rates;

[DataContract]
internal sealed class AccountNumber
{
    [DataMember(Name = "value", EmitDefaultValue = false)]
    public string Value { get; set; } = "";
}
