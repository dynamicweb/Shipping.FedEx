using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models;

[DataContract]
internal sealed class AddressDetails
{
    [DataMember(Name = "streetLines", EmitDefaultValue = false)]
    public IEnumerable<string> StreetLines { get; set; } = [];

    [DataMember(Name = "city", EmitDefaultValue = false)]
    public string City { get; set; } = "";

    [DataMember(Name = "stateOrProvinceCode", EmitDefaultValue = false)]
    public string StateOrProvinceCode { get; set; } = "";

    [DataMember(Name = "postalCode", IsRequired = true)]
    public string PostalCode { get; set; } = "";

    [DataMember(Name = "countryCode", IsRequired = true)]
    public string CountryCode { get; set; } = "";

    [DataMember(Name = "residential", EmitDefaultValue = false)]
    public bool? Residential { get; set; }
}
