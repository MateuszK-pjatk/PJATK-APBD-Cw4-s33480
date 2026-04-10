using System.Collections.Generic;

namespace LegacyRenewalApp
{
    public interface IDiscountCalculator
    {
        DiscountResult CalculateTotalDiscount(Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount, bool useLoyaltyPoints);
    }

    public class DiscountCalculator : IDiscountCalculator
    {
        private readonly IEnumerable<IDiscountRule> _rules;
        
        public DiscountCalculator(IEnumerable<IDiscountRule> rules)
        {
            _rules = rules;
        }

        public DiscountResult CalculateTotalDiscount(Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount, bool useLoyaltyPoints)
        {
            decimal totalDiscount = 0m;
            string totalNotes = string.Empty;

            foreach (var rule in _rules)
            {
                var result = rule.Calculate(customer, plan, seatCount, baseAmount, useLoyaltyPoints);
                totalDiscount += result.Amount;
                totalNotes += result.Note;
            }

            return new DiscountResult { Amount = totalDiscount, Note = totalNotes };
        }
    }
}