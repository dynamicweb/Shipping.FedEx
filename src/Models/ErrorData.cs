using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.GLS.Models;

[DataContract]
internal sealed class ErrorData
{
    [DataMember(Name = "transactionId")]
    public string TransactionId { get; set; } = "";

    [DataMember(Name = "customerTransactionId")]
    public string CustomerTransactionId { get; set; } = "";

    [DataMember(Name = "errors")]
    public IEnumerable<ErrorDetail>? Errors { get; set; }
}
