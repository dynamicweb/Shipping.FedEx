namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Service;

internal enum ApiCommand
{
    /// <summary>
    /// Generates an access token 
    /// See: https://developer.fedex.com/api/en-us/catalog/authorization/v1/docs.html
    /// POST /oauth/token
    /// </summary>
    CreateAccessToken,

    /// <summary>
    /// Generates an access token 
    /// See: https://developer.fedex.com/api/en-us/catalog/rate/v1/docs.html
    /// POST /rate/v1/rates/quotes
    /// </summary>
    GetRateAndTransitTimes,

    /// <summary>
    /// Validates an address
    /// See: https://developer.fedex.com/api/en-us/catalog/address-validation/v1/docs.html
    /// POST /address/v1/addresses/resolve
    /// </summary>
    ValidateAddress
}
