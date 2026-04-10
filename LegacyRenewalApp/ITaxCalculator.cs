namespace LegacyRenewalApp
{
    public interface ITaxCalculator
    {
        decimal GetTaxRate(string country);
    }
}