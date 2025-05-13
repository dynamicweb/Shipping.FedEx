using Dynamicweb.Core;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.AddressValidation;
using Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models.Rates;
using Dynamicweb.Ecommerce.ShippingProviders.GLS.Models;
using System;
using Dynamicweb.Core.Helpers;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Service;

internal sealed class FedExService
{
    public bool DebugLog { get; set; }

    public bool TestMode { get; set; }

    public string ApiKey { get; set; } = "";

    public string SecretKey { get; set; } = "";

    public string Locale { get; set; } = "";

    private TokenData? TokenData { get; set; }

    public RateAndTransitTimesResponse? GetRateAndTransitTimes(Order order, FedEx provider)
    {
        SetAccessToken(DebugLog);

        var configuration = new CommandConfiguration
        {
            CommandType = ApiCommand.GetRateAndTransitTimes,
            TransactionId = order.Id,
            Locale = Locale,
            DebugLog = DebugLog,
            Data = RateRequestCreationHelper.CreateRateAndTransitTimesRequest(order, provider)
        };

        string response = FedExRequest.SendRequest(GetBaseAddress(), configuration, TokenData);

        return Converter.Deserialize<RateAndTransitTimesResponse>(response);
    }

    public ValidateAddressResponse? ValidateAddress(AddressToValidate address, string transactionId)
    {
        SetAccessToken(DebugLog);

        var configuration = new CommandConfiguration
        {
            CommandType = ApiCommand.ValidateAddress,
            TransactionId = transactionId,
            Locale = Locale,
            DebugLog = DebugLog,
            Data = new ValidateAddressRequest
            {
                InEffectAsOfTimestamp = DateTime.Now.ToString(DateHelper.DateOnlyFormatStringSortable),
                AddressesToValidate = [address],
                ValidateAddressControlParameters = new()
                {
                    IncludeResolutionTokens = true
                }
            }
        };

        string response = FedExRequest.SendRequest(GetBaseAddress(), configuration, TokenData);

        return Converter.Deserialize<ValidateAddressResponse>(response);
    }

    private void SetAccessToken(bool debugLog)
    {
        if (!string.IsNullOrWhiteSpace(TokenData?.AccessToken) && TokenData?.ExpiresDate > DateTime.Now)
            return;

        var configuration = new CommandConfiguration
        {
            CommandType = ApiCommand.CreateAccessToken,
            DebugLog = debugLog,
            Parameters =
            {
                ["client_id"] = ApiKey,
                ["client_secret"] = SecretKey,
                ["grant_type"] = "client_credentials"
            }
        };

        string response = FedExRequest.SendRequest(GetBaseAddress(), configuration);

        TokenData = Converter.Deserialize<TokenData>(response);
        if (TokenData is not null)
            TokenData.ExpiresDate = DateTime.Now.AddSeconds(TokenData.ExpiresIn);
    }

    private string GetBaseAddress() => TestMode
        ? "https://apis-sandbox.fedex.com"
        : "https://apis.fedex.com/";
}
