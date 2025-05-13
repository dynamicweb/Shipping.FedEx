using Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.GLS.Models;

[DataContract]
internal sealed class ErrorDetail
{
    [DataMember(Name = "code")]
    public string Code { get; set; } = "";

    [DataMember(Name = "message")]
    public string Message { get; set; } = "";

    [DataMember(Name = "parameterList")]
    public IEnumerable<ParameterData>? Parameters { get; set; }
}
