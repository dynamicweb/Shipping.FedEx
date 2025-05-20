using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.AddressValidation;

[DataContract]
internal sealed class Address
{
    [DataMember(Name = "streetLines", IsRequired = true)]
    public IEnumerable<string> StreetLines { get; set; } = [];

    [DataMember(Name = "city", IsRequired = true)]
    public string City { get; set; } = "";

    [DataMember(Name = "stateOrProvinceCode", EmitDefaultValue = false)]
    public string StateOrProvinceCode { get; set; } = "";

    [DataMember(Name = "postalCode", IsRequired = true)]
    public string PostalCode { get; set; } = "";

    [DataMember(Name = "countryCode", EmitDefaultValue = false)]
    public string CountryCode { get; set; } = "";
}
