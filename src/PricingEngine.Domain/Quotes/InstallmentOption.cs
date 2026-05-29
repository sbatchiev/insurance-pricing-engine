namespace PricingEngine.Domain.Quotes;

public record InstallmentOption(
    int InstallmentCount,
    Money Total,
    IReadOnlyCollection<InstallmentPayment> Schedule);

public record InstallmentPayment(
    int Number,
    DateOnly DueDate,
    Money Amount);
