using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.Rates;

[DataContract]
internal sealed class RequestedPackageLineItem
{
    [DataMember(Name = "groupPackageCount", EmitDefaultValue = false)]
    public int? GroupPackageCount { get; set; }

    [DataMember(Name = "declaredValue", EmitDefaultValue = false)]
    public Money? DeclaredValue { get; set; }

    [DataMember(Name = "weight", IsRequired = true)]
    public Weight? Weight { get; set; }
}
