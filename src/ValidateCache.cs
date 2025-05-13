using Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.AddressValidation;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx;

internal sealed class ValidateCache
{
    public AddressToValidate? Address;
    public ValidateAddressResponse? ValidateResult;
}
