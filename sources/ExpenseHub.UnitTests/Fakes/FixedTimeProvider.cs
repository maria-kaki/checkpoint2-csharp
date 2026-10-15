using System;

namespace ExpenseHub.UnitTests.Fakes;

/// <summary>
/// Relógio fixo para que horários e a regra de data futura sejam determinísticos.
/// </summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    public static readonly DateTimeOffset DefaultNow = new(2026, 10, 10, 15, 30, 0, TimeSpan.Zero);

    private readonly DateTimeOffset _now;

    public FixedTimeProvider()
        : this(DefaultNow)
    {
    }

    public FixedTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }

    public DateTime UtcNow => _now.UtcDateTime;

    public DateOnly Today => DateOnly.FromDateTime(_now.UtcDateTime);

    public override DateTimeOffset GetUtcNow()
    {
        return _now;
    }
}
