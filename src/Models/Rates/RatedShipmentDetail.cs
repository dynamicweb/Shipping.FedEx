using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.Rates;

[DataContract]
internal sealed class RatedShipmentDetail
{
    [DataMember(Name = "rateType")]
    public string RateType { get; set; } = "";

    [DataMember(Name = "ratedWeightMethod")]
    public string RatedWeightMethod { get; set; } = "";

    [DataMember(Name = "quoteNumber")]
    public string QuoteNumber { get; set; } = "";

    [DataMember(Name = "totalDutiesTaxesAndFees")]
    public double TotalDutiesTaxesAndFees { get; set; }

    [DataMember(Name = "totalDiscounts")]
    public double TotalDiscounts { get; set; }

    [DataMember(Name = "totalNetCharge")]
    public double TotalNetCharge { get; set; }
}
