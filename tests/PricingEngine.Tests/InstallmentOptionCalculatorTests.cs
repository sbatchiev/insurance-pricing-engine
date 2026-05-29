using PricingEngine.Domain;
using PricingEngine.Domain.Quotes;

namespace PricingEngine.Tests;

public sealed class InstallmentOptionCalculatorTests
{
    [Fact]
    public void Calculate_distributes_rounding_difference_to_last_installment()
    {
        var calculator = new InstallmentOptionCalculator([1, 2, 4]);

        var options = calculator.Calculate(new Money(100m, "EUR"), new DateOnly(2026, 1, 1));

        var fourInstallments = Assert.Single(options, option => option.InstallmentCount == 4);
        Assert.Equal([25m, 25m, 25m, 25m], fourInstallments.Schedule.Select(payment => payment.Amount.Amount));
        Assert.Equal(100m, fourInstallments.Schedule.Sum(payment => payment.Amount.Amount));
    }

    [Fact]
    public void Calculate_keeps_total_exact_when_amount_does_not_split_evenly()
    {
        var calculator = new InstallmentOptionCalculator([1, 2, 4]);

        var options = calculator.Calculate(new Money(10.01m, "EUR"), new DateOnly(2026, 1, 1));

        var fourInstallments = Assert.Single(options, option => option.InstallmentCount == 4);
        Assert.Equal([2.50m, 2.50m, 2.50m, 2.51m], fourInstallments.Schedule.Select(payment => payment.Amount.Amount));
        Assert.Equal(10.01m, fourInstallments.Schedule.Sum(payment => payment.Amount.Amount));
    }

    [Fact]
    public void Calculate_uses_configured_installment_counts()
    {
        var calculator = new InstallmentOptionCalculator([3]);

        var options = calculator.Calculate(new Money(90m, "EUR"), new DateOnly(2026, 1, 1));

        var option = Assert.Single(options);
        Assert.Equal(3, option.InstallmentCount);
        Assert.Equal([30m, 30m, 30m], option.Schedule.Select(payment => payment.Amount.Amount));
    }

    [Fact]
    public void Constructor_rejects_empty_installment_counts()
    {
        Assert.Throws<ArgumentException>(() => new InstallmentOptionCalculator([]));
    }
}
