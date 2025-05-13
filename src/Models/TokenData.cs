using System;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.GLS.Models;

[DataContract]
internal sealed class TokenData
{
    [DataMember(Name = "token_type")]
    public string TokenType { get; set; } = "";

    [DataMember(Name = "access_token")]
    public string AccessToken { get; set; } = "";

    [DataMember(Name = "expires_in")]
    public int ExpiresIn { get; set; }

    public DateTime ExpiresDate { get; set; }
}
