using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.Rates;

[DataContract]
internal sealed class RateAndTransitTimesResponse
{
    [DataMember(Name = "transactionId")]
    public string TransactionId { get; set; } = "";

    [DataMember(Name = "customerTransactionId")]
    public string CustomerTransactionId { get; set; } = "";

    [DataMember(Name = "output")]
    public RateAndTransitTimesResponseOutput? Output { get; set; }
}
