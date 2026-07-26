using System.Net.Http.Json;
using ECommerce.Application.Interfaces;
using ECommerce.Infrastructure.DTOs;

public class CurrencyConverterService : ICurrencyConverterService
{
    private readonly HttpClient _httpClient;

    public CurrencyConverterService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<decimal> ConvertUsdToBdtAsync(decimal usdAmount)
    {
        try
        {

            string url = "https://api.frankfurter.app/latest?from=USD&to=BDT";

            var response = await _httpClient.GetFromJsonAsync<FrankfurterResponse>(url);

            if (response?.Rates != null && response.Rates.TryGetValue("BDT", out decimal bdtRate))
            {
                return Math.Round(usdAmount * bdtRate, 2);
            }
        }
        catch
        {
            // যদি এপিআই ফেইল করে, একটি ফিক্সড এভারেজ রেট ফলব্যাক হিসেবে রাখতে পারেন (যেমন: ১২৩ টাকা)
            return Math.Round(usdAmount * 123.00m, 2);
        }

        return usdAmount * 123.00m;
    }
}