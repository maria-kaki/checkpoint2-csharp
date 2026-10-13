using ExpenseHub.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseHub.Api.Data.Configurations;

/// <summary>
/// Mapeamento da entidade <see cref="ExpenseHistory"/>.
/// </summary>
public class ExpenseHistoryConfiguration : IEntityTypeConfiguration<ExpenseHistory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExpenseHistory> builder)
    {
        builder.ToTable("ExpenseHistories");
        builder.HasKey(history => history.Id);
        builder.Property(history => history.Id).ValueGeneratedNever();

        builder.Property(history => history.Action)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(history => history.PreviousStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(history => history.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(history => history.Justification)
            .HasMaxLength(ExpenseHistory.JustificationMaxLength);

        builder.Property(history => history.Changes)
            .HasMaxLength(ExpenseHistory.ChangesMaxLength);

        builder.HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(history => history.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(history => new { history.ExpenseId, history.OccurredAtUtc });
    }
}
