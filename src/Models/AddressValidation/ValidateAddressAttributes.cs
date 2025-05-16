using Dynamicweb.Core;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.AddressValidation;

[DataContract]
internal sealed class ValidateAddressAttributes
{    
    public string? CountrySupported { get; set; }

    public string? Resolved { get; set; }

    public bool IsCountrySupported() => Converter.ToBoolean(CountrySupported);

    public bool IsResolved() => Converter.ToBoolean(Resolved);
}
