namespace PricingEngine.Domain.Quotes;

public sealed class InstallmentOptionCalculator
{
    private readonly int[] _installmentCounts;

    public InstallmentOptionCalculator(IEnumerable<int> installmentCounts)
    {
        _installmentCounts = installmentCounts
            .Where(count => count > 0)
            .Distinct()
            .Order()
            .ToArray();

        if (_installmentCounts.Length == 0)
        {
            throw new ArgumentException("At least one installment count must be configured.", nameof(installmentCounts));
        }
    }

    public IReadOnlyCollection<InstallmentOption> Calculate(Money total, DateOnly firstDueDate)
    {
        return _installmentCounts
            .Select(count => CreateOption(total, count, firstDueDate))
            .ToArray();
    }

    private static InstallmentOption CreateOption(Money total, int count, DateOnly firstDueDate)
    {
        var baseAmount = decimal.Round(total.Amount / count, 2, MidpointRounding.AwayFromZero);
        var schedule = new List<InstallmentPayment>(count);
        var allocated = 0m;

        for (var index = 1; index <= count; index++)
        {
            var amount = index == count
                ? total.Amount - allocated
                : baseAmount;

            allocated += amount;
            schedule.Add(new InstallmentPayment(index, firstDueDate.AddMonths(index - 1), new Money(amount, total.Currency)));
        }

        return new InstallmentOption(count, total, schedule);
    }
}
