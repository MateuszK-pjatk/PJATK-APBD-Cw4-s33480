namespace LegacyRenewalApp
{
    public class TaxCalculator : ITaxCalculator
    {
        public decimal GetTaxRate(string country)
        {
            return country switch
            {
                "Poland" => 0.23m,
                "Germany" => 0.19m,
                "Czech Republic" => 0.21m,
                "Norway" => 0.25m,
                _ => 0.20m
            };
        }
    }
}