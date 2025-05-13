using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.AddressValidation;

[DataContract]
internal sealed class ValidateAddressControlParameters
{
    [DataMember(Name = "includeResolutionTokens", EmitDefaultValue = false)]
    public bool IncludeResolutionTokens { get; set; }
}
