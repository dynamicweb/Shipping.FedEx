using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.Rates;

[DataContract]
internal sealed class LocationData
{
    [DataMember(Name = "address", IsRequired = true)]
    public AddressDetails Address { get; set; } = new();
}
