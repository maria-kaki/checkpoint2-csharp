using ExpenseHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseHub.Api.Data.Configurations;

/// <summary>
/// Mapeamento da entidade <see cref="ExpenseCategory"/> com as categorias iniciais.
/// </summary>
public class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("ExpenseCategories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).ValueGeneratedNever();

        builder.Property(category => category.Name)
            .HasMaxLength(ExpenseCategory.NameMaxLength)
            .IsRequired();

        builder.HasIndex(category => category.Name).IsUnique();

        builder.HasData(
            new ExpenseCategory(1, "Alimentação"),
            new ExpenseCategory(2, "Transporte"),
            new ExpenseCategory(3, "Hospedagem"),
            new ExpenseCategory(4, "Material de escritório"),
            new ExpenseCategory(5, "Outros"));
    }
}
