using System;

namespace LegacyRenewalApp
{
    public class FeeResult
    {
        public decimal Amount { get; set; }
        public string Note { get; set; } = string.Empty;
    }
    
    public interface ISupportFeeCalculator
    {
        FeeResult Calculate(bool includePremiumSupport, string normalizedPlanCode);
    }

    public class SupportFeeCalculator : ISupportFeeCalculator
    {
        public FeeResult Calculate(bool includePremiumSupport, string normalizedPlanCode)
        {
            if (!includePremiumSupport) return new FeeResult { Amount = 0m };

            decimal fee = normalizedPlanCode switch
            {
                "START" => 250m,
                "PRO" => 400m,
                "ENTERPRISE" => 700m,
                _ => 0m
            };
            
            return new FeeResult { Amount = fee, Note = "premium support included; " };
        }
    }
    
    public interface IPaymentFeeCalculator
    {
        FeeResult Calculate(string normalizedPaymentMethod, decimal baseAmountForFee);
    }

    public class PaymentFeeCalculator : IPaymentFeeCalculator
    {
        public FeeResult Calculate(string normalizedPaymentMethod, decimal baseAmountForFee)
        {
            return normalizedPaymentMethod switch
            {
                "CARD" => new FeeResult { Amount = baseAmountForFee * 0.02m, Note = "card payment fee; " },
                "BANK_TRANSFER" => new FeeResult { Amount = baseAmountForFee * 0.01m, Note = "bank transfer fee; " },
                "PAYPAL" => new FeeResult { Amount = baseAmountForFee * 0.035m, Note = "paypal fee; " },
                "INVOICE" => new FeeResult { Amount = 0m, Note = "invoice payment; " },
                _ => throw new ArgumentException("Unsupported payment method")
            };
        }
    }
}