using System.Collections.Generic;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx
{
    /// <summary>
    /// Provides methods for caching request data
    /// </summary>
    internal static class ShippingProviderHelper
    {

        #region Nested types

        /// <summary>
        /// Structure that is used while calculating shipping fee for the specified order in FedEx shipping provider
        /// </summary>
        internal struct RateRequest
        {
            public string Request;
            public double Rate;
            public string Currency;
            public List<string> Errors;
            public List<string> Warning;
        }

        #endregion

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
        public static RateRequest CheckIsRateRequestCached(string shippingID, string request)
        {
            var rateRequest = default(RateRequest);

            if ((Context.Current.Session[ShippingCacheKey(shippingID)] != null))
            {
                var cachedRequest = (RateRequest)Context.Current.Session[ShippingCacheKey(shippingID)];
                if (request == cachedRequest.Request)
                {
                    rateRequest = cachedRequest;
                }
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
            Context.Current.Session[ShippingCacheKey(shippingID)] = new RateRequest
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
        {
            return Context.Current.Items.Contains(ShippingCacheKey(shippingID));
        }

        /// <summary>
        /// Marks shipping request status as "in progress"
        /// </summary>
        /// <param name="shippingID"></param>
        public static void SetShippingRequestIsProcessed(string shippingID)
        {
            if (!IsThisShippingRequestWasProcessed(shippingID))
            {
                Context.Current.Items.Add(ShippingCacheKey(shippingID), true);
            }
        }

        #endregion

    }

}
