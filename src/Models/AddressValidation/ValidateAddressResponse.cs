using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.AddressValidation;

[DataContract]
internal sealed class ValidateAddressResponse
{
    [DataMember(Name = "transactionId")]
    public string TransactionId { get; set; } = "";

    [DataMember(Name = "customerTransactionId")]
    public string CustomerTransactionId { get; set; } = "";

    [DataMember(Name = "output")]
    public ValidateAddressResponseOutput? Output { get; set; }
}