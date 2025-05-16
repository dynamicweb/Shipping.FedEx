using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.AddressValidation;

[DataContract]
internal sealed class ValidateAddressRequest
{
    [DataMember(Name = "inEffectAsOfTimestamp", EmitDefaultValue = false)]
    public string InEffectAsOfTimestamp { get; set; } = "";

    [DataMember(Name = "validateAddressControlParameters", EmitDefaultValue = false)]
    public ValidateAddressControlParameters? ValidateAddressControlParameters { get; set; }

    [DataMember(Name = "addressesToValidate", IsRequired = true)]
    public IEnumerable<AddressToValidate> AddressesToValidate { get; set; } = [];
}
