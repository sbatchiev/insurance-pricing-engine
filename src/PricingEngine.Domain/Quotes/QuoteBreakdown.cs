namespace PricingEngine.Domain.Quotes;

public record QuoteBreakdown(
    Money NetPremium,
    Money Taxes,
    Money Fees)
{
    public Money Total
    {
        get
        {
            if (Taxes.Currency != NetPremium.Currency || Fees.Currency != NetPremium.Currency)
            {
                throw new InvalidOperationException("Quote breakdown amounts must use the same currency.");
            }

            return new Money(NetPremium.Amount + Taxes.Amount + Fees.Amount, NetPremium.Currency);
        }
    }
}
