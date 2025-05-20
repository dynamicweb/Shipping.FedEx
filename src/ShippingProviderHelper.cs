using System;
using System.Collections.Generic;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx;

/// <summary>
/// Provides methods for caching request data
/// </summary>
internal static class ShippingProviderHelper
{
    #region Cache rate request

    private static string ShippingCacheKey(string shippingID)
    {
        return string.Format("ShippingServiceRateRequest_{0}", shippingID);
    }

    /// <summary>
    /// Checks if rate request is cached
    /// </summary>
    /// <param name="shippingID">identifier of shipping</param>
    /// <param name="request">request</param>
    /// <returns>Rate request instance</returns>
    public static CachedRateRequestData CheckIsRateRequestCached(string shippingID, string request)
    {
        var rateRequest = default(CachedRateRequestData);

        if (Context.Current?.Session is null)
            return rateRequest;

        if (Context.Current.Session[ShippingCacheKey(shippingID)] is object cachedData)
        {
            var cachedRequest = (CachedRateRequestData)cachedData;
            if (string.Equals(request, cachedRequest.Request, StringComparison.Ordinal))
                rateRequest = cachedRequest;
        }

        return rateRequest;
    }

    /// <summary>
    /// Adds rate request to cache
    /// </summary>
    /// <param name="shippingID">identifier of shipping</param>
    /// <param name="request">Request</param>
    /// <param name="rate">Rate</param>
    /// <param name="currency">Currency</param>
    /// <param name="warning">list of warnings</param>
    /// <param name="errors">list of errors</param>
    public static void CacheRateRequest(string shippingID, string request, double rate, string currency, List<string> warning, List<string> errors)
    {
        if (Context.Current?.Session is null)
            return;

        Context.Current.Session[ShippingCacheKey(shippingID)] = new CachedRateRequestData
        {
            Request = request,
            Rate = rate,
            Currency = currency,
            Warning = new List<string>(warning),
            Errors = new List<string>(errors)
        };
    }

    /// <summary>
    /// Gets information about shipping request processing status
    /// </summary>
    /// <param name="shippingID">identifier of shipping</param>
    /// <returns>true if shipping was processed</returns>
    public static bool IsThisShippingRequestWasProcessed(string shippingID)
        => Context.Current?.Items?.Contains(ShippingCacheKey(shippingID)) is true;

    /// <summary>
    /// Marks shipping request status as "in progress"
    /// </summary>
    /// <param name="shippingID"></param>
    public static void SetShippingRequestIsProcessed(string shippingID)
    {
        if (Context.Current?.Items is null)
            return;

        if (!IsThisShippingRequestWasProcessed(shippingID))
            Context.Current.Items.Add(ShippingCacheKey(shippingID), true);
    }

    #endregion

}
