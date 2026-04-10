namespace LegacyRenewalApp
{
    public class DiscountResult
    {
        public decimal Amount { get; set; }
        public string Note { get; set; } = string.Empty;
    }
    
    public interface IDiscountRule
    {
        DiscountResult Calculate(Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount, bool useLoyaltyPoints);
    }
    
    public class SegmentDiscountRule : IDiscountRule
    {
        public DiscountResult Calculate(Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount, bool useLoyaltyPoints)
        {
            if (customer.Segment == "Platinum") return new DiscountResult { Amount = baseAmount * 0.15m, Note = "platinum discount; " };
            if (customer.Segment == "Gold") return new DiscountResult { Amount = baseAmount * 0.10m, Note = "gold discount; " };
            if (customer.Segment == "Silver") return new DiscountResult { Amount = baseAmount * 0.05m, Note = "silver discount; " };
            if (customer.Segment == "Education" && plan.IsEducationEligible) return new DiscountResult { Amount = baseAmount * 0.20m, Note = "education discount; " };
            return new DiscountResult { Amount = 0 };
        }
    }
    
    public class LoyaltyYearsDiscountRule : IDiscountRule
    {
        public DiscountResult Calculate(Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount, bool useLoyaltyPoints)
        {
            if (customer.YearsWithCompany >= 5) return new DiscountResult { Amount = baseAmount * 0.07m, Note = "long-term loyalty discount; " };
            if (customer.YearsWithCompany >= 2) return new DiscountResult { Amount = baseAmount * 0.03m, Note = "basic loyalty discount; " };
            return new DiscountResult { Amount = 0 };
        }
    }
    
    public class VolumeDiscountRule : IDiscountRule
    {
        public DiscountResult Calculate(Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount, bool useLoyaltyPoints)
        {
            if (seatCount >= 50) return new DiscountResult { Amount = baseAmount * 0.12m, Note = "large team discount; " };
            if (seatCount >= 20) return new DiscountResult { Amount = baseAmount * 0.08m, Note = "medium team discount; " };
            if (seatCount >= 10) return new DiscountResult { Amount = baseAmount * 0.04m, Note = "small team discount; " };
            return new DiscountResult { Amount = 0 };
        }
    }
    
    public class LoyaltyPointsDiscountRule : IDiscountRule
    {
        public DiscountResult Calculate(Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount, bool useLoyaltyPoints)
        {
            if (useLoyaltyPoints && customer.LoyaltyPoints > 0)
            {
                int pointsToUse = customer.LoyaltyPoints > 200 ? 200 : customer.LoyaltyPoints;
                return new DiscountResult { Amount = pointsToUse, Note = $"loyalty points used: {pointsToUse}; " };
            }
            return new DiscountResult { Amount = 0 };
        }
    }
}