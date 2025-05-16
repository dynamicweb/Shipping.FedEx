using Dynamicweb.Core;
using Dynamicweb.Ecommerce.ShippingProviders.FedEx.Models;
using Dynamicweb.Ecommerce.ShippingProviders.GLS.Models;
using Dynamicweb.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Dynamicweb.Ecommerce.ShippingProviders.FedEx.Service;

internal static class FedExRequest
{
    public static string SendRequest(string baseAddress, CommandConfiguration configuration, TokenData? tokenData)
    {
        using HttpMessageHandler messageHandler = GetMessageHandler();
        using var client = new HttpClient(messageHandler);

        client.BaseAddress = new Uri(baseAddress);
        client.Timeout = new TimeSpan(0, 0, 0, 90);

        if (tokenData is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(tokenData.TokenType, tokenData.AccessToken);
        if (!string.IsNullOrEmpty(configuration.TransactionId))
            client.DefaultRequestHeaders.Add("x-customer-transaction-id", configuration.TransactionId);
        if (!string.IsNullOrEmpty(configuration.Locale))
            client.DefaultRequestHeaders.Add("x-locale", configuration.Locale);

        string apiCommand = GetCommandLink(baseAddress, configuration);
        Task<HttpResponseMessage> requestTask = configuration.CommandType switch
        {
            //POST                 
            ApiCommand.CreateAccessToken => client.PostAsync(apiCommand, GetFormUrlEncodedContent(configuration)),
            ApiCommand.GetRateAndTransitTimes or
            ApiCommand.ValidateAddress => client.PostAsync(apiCommand, GetStringContent(configuration)),
            _ => throw new NotImplementedException($"Unknown operation was used. The operation code: {configuration.CommandType}.")
        };

        try
        {
            using HttpResponseMessage response = requestTask.GetAwaiter().GetResult();

            string responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (configuration.DebugLog)
            {
                var logText = new StringBuilder("Remote server response:");
                logText.AppendLine($"HttpStatusCode = {response.StatusCode}");
                logText.AppendLine($"HttpStatusDescription = {response.ReasonPhrase}");
                logText.AppendLine($"Response text: {responseText}");

                Log(logText.ToString(), false, configuration.CommandType);               
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = Converter.Deserialize<ErrorData>(responseText);
                string errorMessage = GetErrorMessage(errorResponse);

                if (string.IsNullOrWhiteSpace(errorMessage))
                    errorMessage = $"Unhandled exception. Operation failed: {response.ReasonPhrase}. Response text: {responseText}";

                Log(errorMessage, false, configuration.CommandType);

                throw new Exception(errorMessage);
            }

            return responseText;
        }
        catch (HttpRequestException requestException)
        {
            string errorMessage = $"An error occurred during FedEx request. Error code: {requestException.StatusCode}";
            Log(errorMessage, false, configuration.CommandType);

            throw new Exception(errorMessage);
        }

        HttpMessageHandler GetMessageHandler() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
        };
    }   

    private static HttpContent GetStringContent(CommandConfiguration configuration)
    {
        string content = Converter.SerializeCompact(configuration.Data);

        if (configuration.DebugLog)
            Log($"Request data: {content}", true, configuration.CommandType);

        return new StringContent(content, Encoding.UTF8, "application/json");
    }

    private static FormUrlEncodedContent? GetFormUrlEncodedContent(CommandConfiguration configuration)
    {
        if (configuration.Parameters?.Count is null or 0)
            return null;

        Dictionary<string, string> convertedParameters = configuration.Parameters
            .Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value?.ToString() ?? ""))
            .ToDictionary(StringComparer.OrdinalIgnoreCase);

        if (configuration.DebugLog)
        {
            var message = new StringBuilder("Request data:");
            foreach ((string key, string value) in convertedParameters)
                message.AppendLine($"{key} = '{value}'");

            Log(message.ToString(), true, configuration.CommandType);
        }

        return new FormUrlEncodedContent(convertedParameters);
    }

    private static string GetCommandLink(string baseAddress, CommandConfiguration configuration)
    {
        return configuration.CommandType switch
        {
            ApiCommand.CreateAccessToken => GetCommandLink("oauth/token"),
            ApiCommand.GetRateAndTransitTimes => GetCommandLink("rate/v1/rates/quotes"),
            ApiCommand.ValidateAddress => GetCommandLink($"address/v1/addresses/resolve"),
            _ => throw new NotImplementedException($"The api command is not supported. Command: {configuration.CommandType}")
        };

        string GetCommandLink(string gateway) => $"{baseAddress}/{gateway}";
    }

    private static string GetErrorMessage(ErrorData? errorResponse)
    {
        if (errorResponse?.Errors?.Any() is null or false)
            return string.Empty;

        var errorMessage = new StringBuilder("FedEx returned an error.");
        foreach (ErrorDetail error in errorResponse.Errors)
        {
            errorMessage.AppendLine($"Error code: {error.Code}.");
            if (!string.IsNullOrWhiteSpace(error.Message))
                errorMessage.AppendLine($"Error message: {error.Message}.");

            if (error.Parameters?.Any() is true)
            {
                errorMessage.AppendLine($"Please check the following properties:");
                foreach (ParameterData parameter in error.Parameters)
                    errorMessage.AppendLine($"{parameter.Key}: '{parameter.Value}'");
            }
        }

        return errorMessage.ToString();
    }

    private static void Log(string message, bool isRequest, ApiCommand command)
    {
        string type = isRequest ? "Request" : "Response";
        var errorMessage = new StringBuilder($"{type} for command: '{command}'.");
        errorMessage.AppendLine(message);

        if (command is ApiCommand.ValidateAddress)
            LogAddressValidator(message);
        else
            LogFedEx(message, isRequest);
    }
  
    private static void LogFedEx(string message, bool isRequest)
    {
        string text = typeof(FedEx).FullName ?? "ShippingProvider";
        string name = "/eCom/ShippingProvider/" + text + "/" + (isRequest ? "Request" : "Response");
        LogManager.Current.GetLogger(name).Info(message);
        LogManager.System.GetLogger("Provider", text).Info(message);
    }

    private static void LogAddressValidator(string message)
    {
        string name = typeof(FedexAddressValidationProvider).FullName ?? "AddressValidationProvider";
        LogManager.Current.GetLogger(string.Format("/eCom/AddressValidatorProvider/{0}", name)).Info(message);
        LogManager.System.GetLogger(LogCategory.Provider, name).Info(message);
    }
}
