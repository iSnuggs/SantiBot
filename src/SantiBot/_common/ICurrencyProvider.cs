using System.Globalization;
using System.Numerics;

namespace SantiBot.Common;

public interface ICurrencyProvider
{
    string GetCurrencySign();
}

public static class CurrencyHelper
{
    public static string N<T>(T cur, IFormatProvider format)
        where T : INumber<T>
        => cur.ToString("C0", format);

    public static string N<T>(T cur, CultureInfo culture, string currencySign)
        where T : INumber<T>
        => N(cur, GetCurrencyFormat(culture, currencySign));

    private static IFormatProvider GetCurrencyFormat(CultureInfo culture, string currencySign)
    {
        var currencyCulture = (CultureInfo)culture.Clone();
        currencyCulture.NumberFormat.CurrencySymbol = currencySign;
        currencyCulture.NumberFormat.CurrencyNegativePattern = 5;

        return currencyCulture;
    }
}