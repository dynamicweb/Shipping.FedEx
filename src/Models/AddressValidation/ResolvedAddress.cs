using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.AddressValidation;

[DataContract]
internal sealed class ResolvedAddress
{
    [DataMember(Name = "streetLinesToken")]
    public IEnumerable<string>? StreetLinesToken { get; set; }

    [DataMember(Name = "cityToken", EmitDefaultValue = false)]
    public IEnumerable<ResolvedValue>? СityToken { get; set; }

    [DataMember(Name = "stateOrProvinceCode", EmitDefaultValue = false)]
    public string? StateOrProvinceCode { get; set; }

    [DataMember(Name = "countryCode", EmitDefaultValue = false)]
    public string? CountryCode { get; set; }

    [DataMember(Name = "customerMessage")]
    public IEnumerable<string>? CustomerMessage { get; set; }

    [DataMember(Name = "postalCodeToken")]
    public ResolvedValue? PostalCodeToken { get; set; }

    [DataMember(Name = "classification", EmitDefaultValue = false)]
    public string? Classification { get; set; }

    [DataMember(Name = "postOfficeBox")]
    public bool PostOfficeBox { get; set; }

    [DataMember(Name = "normalizedStatusNameDPV")]
    public bool NormalizedStatusNameDPV { get; set; }

    [DataMember(Name = "standardizedStatusNameMatchSource", EmitDefaultValue = false)]
    public string? StandardizedStatusNameMatchSource { get; set; }

    [DataMember(Name = "resolutionMethodName", EmitDefaultValue = false)]
    public string? ResolutionMethodName { get; set; }

    [DataMember(Name = "ruralRouteHighwayContract")]
    public bool RuralRouteHighwayContract { get; set; }

    [DataMember(Name = "generalDelivery")]
    public bool GeneralDelivery { get; set; }

    [DataMember(Name = "attributes")]
    public ValidateAddressAttributes? Attributes { get; set; }
}
