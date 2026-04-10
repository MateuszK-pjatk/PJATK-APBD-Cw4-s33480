using System;

namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ISubscriptionPlanRepository _planRepository;
        private readonly IBillingGateway _billingGateway;
        private readonly ITaxCalculator _taxCalculator;
        private readonly IDiscountCalculator _discountCalculator;
        
        public SubscriptionRenewalService() : this(
            new CustomerRepository(), 
            new SubscriptionPlanRepository(), 
            new BillingGatewayAdapter(),
            new TaxCalculator(),
            new DiscountCalculator(new IDiscountRule[] 
            {
                new SegmentDiscountRule(),
                new LoyaltyYearsDiscountRule(),
                new VolumeDiscountRule(),
                new LoyaltyPointsDiscountRule()
            }))
        {
        }
        
        public SubscriptionRenewalService(
            ICustomerRepository customerRepository,
            ISubscriptionPlanRepository planRepository,
            IBillingGateway billingGateway,
            ITaxCalculator taxCalculator,
            IDiscountCalculator discountCalculator)
        {
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
            _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
            _billingGateway = billingGateway ?? throw new ArgumentNullException(nameof(billingGateway));
            _taxCalculator = taxCalculator ?? throw new ArgumentNullException(nameof(taxCalculator));
            _discountCalculator = discountCalculator ?? throw new ArgumentNullException(nameof(discountCalculator));
        }
        
        public RenewalInvoice CreateRenewalInvoice(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints)
        {
            if (customerId <= 0)
            {
                throw new ArgumentException("Customer id must be positive");
            }

            if (string.IsNullOrWhiteSpace(planCode))
            {
                throw new ArgumentException("Plan code is required");
            }

            if (seatCount <= 0)
            {
                throw new ArgumentException("Seat count must be positive");
            }

            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                throw new ArgumentException("Payment method is required");
            }

            string normalizedPlanCode = planCode.Trim().ToUpperInvariant();
            string normalizedPaymentMethod = paymentMethod.Trim().ToUpperInvariant();
            
            var customer = _customerRepository.GetById(customerId);
            var plan = _planRepository.GetByCode(normalizedPlanCode);
            

            if (!customer.IsActive)
            {
                throw new InvalidOperationException("Inactive customers cannot renew subscriptions");
            }

            decimal baseAmount = (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;
            var discountResult = _discountCalculator.CalculateTotalDiscount(customer, plan, seatCount, baseAmount, useLoyaltyPoints);
            decimal discountAmount = discountResult.Amount;
            string notes = discountResult.Note;
            
            decimal subtotalAfterDiscount = baseAmount - discountAmount;
            if (subtotalAfterDiscount < 300m)
            {
                subtotalAfterDiscount = 300m;
                notes += "minimum discounted subtotal applied; ";
            }
            
            decimal supportFee = 0m;
            if (includePremiumSupport)
            {
                if (normalizedPlanCode == "START") supportFee = 250m;
                else if (normalizedPlanCode == "PRO") supportFee = 400m;
                else if (normalizedPlanCode == "ENTERPRISE") supportFee = 700m;
                notes += "premium support included; ";
            }
            
            decimal paymentFee = 0m;
            if (normalizedPaymentMethod == "CARD") { paymentFee = (subtotalAfterDiscount + supportFee) * 0.02m; notes += "card payment fee; "; }
            else if (normalizedPaymentMethod == "BANK_TRANSFER") { paymentFee = (subtotalAfterDiscount + supportFee) * 0.01m; notes += "bank transfer fee; "; }
            else if (normalizedPaymentMethod == "PAYPAL") { paymentFee = (subtotalAfterDiscount + supportFee) * 0.035m; notes += "paypal fee; "; }
            else if (normalizedPaymentMethod == "INVOICE") { paymentFee = 0m; notes += "invoice payment; "; }
            else throw new ArgumentException("Unsupported payment method");
            
            decimal taxRate = _taxCalculator.GetTaxRate(customer.Country);
            decimal taxBase = subtotalAfterDiscount + supportFee + paymentFee;
            decimal taxAmount = taxBase * taxRate;
            
            decimal finalAmount = taxBase + taxAmount;
            if (finalAmount < 500m)
            {
                finalAmount = 500m;
                notes += "minimum invoice amount applied; ";
            }

            var invoice = new RenewalInvoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{customerId}-{normalizedPlanCode}",
                CustomerName = customer.FullName,
                PlanCode = normalizedPlanCode,
                PaymentMethod = normalizedPaymentMethod,
                SeatCount = seatCount,
                BaseAmount = Math.Round(baseAmount, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero),
                SupportFee = Math.Round(supportFee, 2, MidpointRounding.AwayFromZero),
                PaymentFee = Math.Round(paymentFee, 2, MidpointRounding.AwayFromZero),
                TaxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero),
                FinalAmount = Math.Round(finalAmount, 2, MidpointRounding.AwayFromZero),
                Notes = notes.Trim(),
                GeneratedAt = DateTime.UtcNow
            };

            _billingGateway.SaveInvoice(invoice);

            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                string subject = "Subscription renewal invoice";
                string body =
                    $"Hello {customer.FullName}, your renewal for plan {normalizedPlanCode} " +
                    $"has been prepared. Final amount: {invoice.FinalAmount:F2}.";

                _billingGateway.SendEmail(customer.Email, subject, body);
            }

            return invoice;
        }
    }
}
