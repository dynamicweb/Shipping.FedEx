using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.Rates;

[DataContract]
internal sealed class Money
{
    [DataMember(Name = "amount", EmitDefaultValue = false)]
    public double Amount { get; set; }

    [DataMember(Name = "currency", EmitDefaultValue = false)]
    public string Currency { get; set; } = "";
}
