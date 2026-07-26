namespace ECommerce.Application.Interfaces;
public interface ICurrencyConverterService
{
    Task<decimal> ConvertUsdToBdtAsync(decimal usdAmount);
}