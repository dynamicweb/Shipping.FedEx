using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.Rates;

[DataContract]
internal sealed class RateAndTransitTimesResponseOutput
{
    [DataMember(Name = "rateReplyDetails")]
    public IEnumerable<RateReplyDetail>? RateReplyDetails { get; set; }

    [DataMember(Name = "quoteDate")]
    public string QuoteDate { get; set; } = "";

    [DataMember(Name = "isEncoded")]
    public bool IsEncoded { get; set; }

    [DataMember(Name = "alerts")]
    public IEnumerable<Alert>? Alerts { get; set; }
}
