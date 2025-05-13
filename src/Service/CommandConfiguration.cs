using System;
using System.Collections.Generic;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Service;

internal sealed class CommandConfiguration
{
    /// <summary>
    /// Indicates if request and response must be logged
    /// </summary>
    public bool DebugLog { get; set; }

    /// <summary>
    /// The unique customer's transaction id
    /// </summary>
    public string TransactionId { get; set; } = "";

    /// <summary>
    /// Indicates the combination of language code and country code
    /// </summary>
    public string Locale { get; set; } = "";

    /// <summary>
    /// FeedEx command. See operation urls in <see cref="FedExRequest"/> and <see cref="ApiCommand"/>
    /// </summary>
    public ApiCommand CommandType { get; set; }

    /// <summary>
    /// The data object to serialize into JSON. Most of requests use application/json content type of data.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Parameters for x-www-form-urlencoded data
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
}
