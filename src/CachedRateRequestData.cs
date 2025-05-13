using System.Collections.Generic;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx;

/// <summary>
/// Structure that is used while calculating shipping fee for the specified order in FedEx shipping provider
/// </summary>
internal struct CachedRateRequestData
{
    public string Request;
    public double Rate;
    public string Currency;
    public List<string> Errors;
    public List<string> Warning;
}
