using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.AddressValidation;

[DataContract]
internal sealed class AddressToValidate
{
    [DataMember(Name = "clientReferenceId", EmitDefaultValue = false)]
    public string ClientReferenceId { get; set; } = "";

    [DataMember(Name = "address", IsRequired = true)]
    public Address Address { get; set; } = new();
}
