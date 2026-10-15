using System;
using System.Globalization;
using ExpenseHub.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

/// <summary>
/// Contrato de validação de descrição, valor e data.
/// </summary>
[TestClass]
public sealed class ExpenseRulesTests
{
    private static readonly DateOnly _today = new(2026, 10, 10);

    /// <summary>O valor deve estar entre R$ 0,01 e Int32.MaxValue, inclusive.</summary>
    /// <param name="amount">Valor em texto invariável.</param>
    /// <param name="valid">Resultado esperado.</param>
    [TestMethod]
    [DataRow("0.01", true)]
    [DataRow("2147483647", true)]
    [DataRow("0", false)]
    [DataRow("0.009", false)]
    [DataRow("-10", false)]
    [DataRow("2147483647.01", false)]
    public void Validate_LimitesDoValor(string amount, bool valid)
    {
        var errors = ExpenseRules.Validate(
            "Descrição válida",
            decimal.Parse(amount, CultureInfo.InvariantCulture),
            _today,
            _today);

        Assert.AreEqual(valid, !errors.ContainsKey("amount"));
    }

    /// <summary>A descrição deve ter entre 10 e 500 caracteres.</summary>
    /// <param name="length">Quantidade de caracteres.</param>
    /// <param name="valid">Resultado esperado.</param>
    [TestMethod]
    [DataRow(10, true)]
    [DataRow(500, true)]
    [DataRow(9, false)]
    [DataRow(501, false)]
    [DataRow(0, false)]
    public void Validate_LimitesDaDescricao(int length, bool valid)
    {
        var errors = ExpenseRules.Validate(new string('a', length), 10m, _today, _today);

        Assert.AreEqual(valid, !errors.ContainsKey("description"));
    }

    /// <summary>Espaços nas pontas não contam para o tamanho mínimo da descrição.</summary>
    [TestMethod]
    public void Validate_DescricaoCompletadaComEspacos_EhInvalida()
    {
        var errors = ExpenseRules.Validate("   curta    ", 10m, _today, _today);

        Assert.IsTrue(errors.ContainsKey("description"));
    }

    /// <summary>A data da despesa pode ser hoje ou passada, mas não futura.</summary>
    /// <param name="daysFromToday">Deslocamento em dias a partir de hoje.</param>
    /// <param name="valid">Resultado esperado.</param>
    [TestMethod]
    [DataRow(0, true)]
    [DataRow(-30, true)]
    [DataRow(1, false)]
    public void Validate_DataDaDespesa(int daysFromToday, bool valid)
    {
        var errors = ExpenseRules.Validate("Descrição válida", 10m, _today.AddDays(daysFromToday), _today);

        Assert.AreEqual(valid, !errors.ContainsKey("expenseDate"));
    }

    /// <summary>Dados válidos não produzem erros.</summary>
    [TestMethod]
    public void Validate_DadosValidos_SemErros()
    {
        var errors = ExpenseRules.Validate("Almoço com cliente", 120.35m, _today, _today);

        Assert.IsEmpty(errors);
    }
}
