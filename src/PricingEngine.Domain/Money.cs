namespace PricingEngine.Domain;

public readonly record struct Money
{
    public Money(decimal amount, string currency)
    {
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency;
    }

    public decimal Amount { get; }
    public string Currency { get; }
}
