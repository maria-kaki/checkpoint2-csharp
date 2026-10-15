using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Garante que instantes lidos do banco sejam marcados como UTC. O SQLite não guarda o
/// <see cref="DateTimeKind"/>, e todos os horários da aplicação são gravados em UTC.
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    /// <summary>
    /// Cria o conversor.
    /// </summary>
    public UtcDateTimeConverter()
        : base(
            value => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}
