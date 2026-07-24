using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Payments.DTOs;
using ECommerce.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Payments;

public class BkashPaymentProcessor : IPaymentProcessor
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BkashPaymentProcessor> _logger;

    public BkashPaymentProcessor(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<BkashPaymentProcessor> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public PaymentProvider Provider => PaymentProvider.Bkash;

    // ১. Create Payment (Checkout URL জেনারেট করা)
    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        try
        {
            var token = await GrantTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return new PaymentResult(
                    IsSuccess: false,
                    ErrorMessage: "Failed to authenticate with bKash API.",
                    RedirectUrl: null,
                    TransactionId: null,
                    RawResponse: null!
                );
            }

            var paymentRequestMessage = new HttpRequestMessage(HttpMethod.Post, _configuration["Bkash:BaseUrl"]! + "/tokenized/checkout/create");
            paymentRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(token!);
            paymentRequestMessage.Headers.Add("X-APP-Key", _configuration["Bkash:AppKey"]!);

            var body = new
            {
                mode = "0011",
                payerReference = "01700000000",
                callbackURL = _configuration["Bkash:CallbackUrl"]!,
                amount = request.Amount.ToString("0.00"),
                currency = "BDT",
                intent = "sale",
                merchantInvoiceNumber = request.OrderId.ToString()
            };
            paymentRequestMessage.Content = JsonContent.Create(body);

            var response = await _httpClient.SendAsync(paymentRequestMessage);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("bKash Create Payment Request Failed: {Error}", responseContent);
                return new PaymentResult(
                    IsSuccess: false,
                    ErrorMessage: "bKash Create Payment API failed.",
                    RedirectUrl: null,
                    TransactionId: null,
                    RawResponse: responseContent
                );
            }

            using var doc = ParseJsonSafely(responseContent);
            var root = doc.RootElement;

            var statusCode = root.TryGetProperty("statusCode", out var statusProp) ? statusProp.GetString() : null;
            if (statusCode == "0000")
            {
                var bkashUrl = root.GetProperty("bkashURL").GetString()!;
                var paymentId = root.GetProperty("paymentID").GetString()!;

                return new PaymentResult(
                    IsSuccess: true,
                    ErrorMessage: null,
                    RedirectUrl: bkashUrl,
                    TransactionId: paymentId,
                    RawResponse: root.GetRawText()
                );
            }

            var statusMessage = root.TryGetProperty("statusMessage", out var msgProp) ? msgProp.GetString() : "Payment creation failed";
            return new PaymentResult(
                IsSuccess: false,
                ErrorMessage: statusMessage,
                RedirectUrl: null,
                TransactionId: null,
                RawResponse: root.GetRawText()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bKash payment for OrderId: {OrderId}", request.OrderId);
            return new PaymentResult(
                IsSuccess: false,
                ErrorMessage: "An unexpected error occurred while processing bKash payment.",
                RedirectUrl: null,
                TransactionId: null,
                RawResponse: null!
            );
        }
    }

    // ২. Complete / Execute Payment
    public async Task<PaymentResult> CompletePaymentAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GrantTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return new PaymentResult(
                    IsSuccess: false,
                    ErrorMessage: "Failed to authenticate with bKash API during execution.",
                    RedirectUrl: null,
                    TransactionId: null,
                    RawResponse: null!
                );
            }

            var request = new HttpRequestMessage(HttpMethod.Post, _configuration["Bkash:BaseUrl"]! + "/tokenized/checkout/execute");
            request.Headers.Authorization = new AuthenticationHeaderValue(token!);
            request.Headers.Add("X-APP-Key", _configuration["Bkash:AppKey"]!);

            request.Content = JsonContent.Create(new { paymentID = paymentId });

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new PaymentResult(
                    IsSuccess: false,
                    ErrorMessage: "bKash Execute Payment API request failed.",
                    RedirectUrl: null,
                    TransactionId: null,
                    RawResponse: responseContent
                );
            }

            using var doc = ParseJsonSafely(responseContent);
            var root = doc.RootElement;

            var statusCode = root.TryGetProperty("statusCode", out var statusProp) ? statusProp.GetString() : null;
            var transactionStatus = root.TryGetProperty("transactionStatus", out var trxProp) ? trxProp.GetString() : null;

            if (statusCode == "0000" && transactionStatus == "Completed")
            {
                var trxId = root.GetProperty("trxID").GetString()!;

                return new PaymentResult(
                    IsSuccess: true,
                    ErrorMessage: null,
                    RedirectUrl: null,
                    TransactionId: trxId,
                    RawResponse: root.GetRawText()
                );
            }

            var statusMessage = root.TryGetProperty("statusMessage", out var msgProp) ? msgProp.GetString() : "Payment execution was not successful.";
            return new PaymentResult(
                IsSuccess: false,
                ErrorMessage: statusMessage,
                RedirectUrl: null,
                TransactionId: null,
                RawResponse: root.GetRawText()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing bKash payment for PaymentId: {PaymentId}", paymentId);
            return new PaymentResult(
                IsSuccess: false,
                ErrorMessage: "An unexpected error occurred while executing bKash payment.",
                RedirectUrl: null,
                TransactionId: null,
                RawResponse: null!
            );
        }
    }

    // Helper Method: Grant Token
    private async Task<string?> GrantTokenAsync()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _configuration["Bkash:BaseUrl"]! + "/tokenized/checkout/token/grant");
            request.Headers.Add("username", _configuration["Bkash:Username"]!);
            request.Headers.Add("password", _configuration["Bkash:Password"]!);

            var body = new
            {
                app_key = _configuration["Bkash:AppKey"]!,
                app_secret = _configuration["Bkash:AppSecret"]!
            };

            request.Content = JsonContent.Create(body);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("bKash Grant Token HTTP Error {Status}: {Body}", response.StatusCode, responseContent);
                return null;
            }

            using var doc = ParseJsonSafely(responseContent);
            var root = doc.RootElement;

            if (root.TryGetProperty("id_token", out var tokenProp))
            {
                return tokenProp.GetString();
            }

            _logger.LogError("bKash Grant Token Response missing 'id_token': {Raw}", root.GetRawText());
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in bKash GrantTokenAsync");
            return null;
        }
    }

    // Unescaped JSON String Safe Parser
    private static JsonDocument ParseJsonSafely(string jsonString)
    {
        var sanitizedJson = jsonString.Replace("\r", "").Replace("\n", "");
        return JsonDocument.Parse(sanitizedJson);
    }
}