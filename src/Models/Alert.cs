using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models;

[DataContract]
internal sealed class Alert
{
    [DataMember(Name = "code")]
    public string Code { get; set; } = "";

    [DataMember(Name = "message")]
    public string Message { get; set; } = "";

    [DataMember(Name = "alertType")]
    public bool AlertType { get; set; }
}
