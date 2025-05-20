using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.Rates;

[DataContract]
internal sealed class CustomerMessage
{
    [DataMember(Name = "code")]
    public string Code { get; set; } = "";

    [DataMember(Name = "message")]
    public string Message { get; set; } = "";
}
