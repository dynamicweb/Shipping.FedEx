using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.AddressValidation;

[DataContract]
internal sealed class ValidateAddressResponseOutput
{
    [DataMember(Name = "resolvedAddresses")]
    public IEnumerable<ResolvedAddress>? ResolvedAddresses { get; set; }

    [DataMember(Name = "alerts")]
    public IEnumerable<Alert>? Alerts { get; set; }
}
