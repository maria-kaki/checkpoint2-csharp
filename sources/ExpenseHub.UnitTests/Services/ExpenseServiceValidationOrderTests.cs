using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Responses;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

/// <summary>Ordem das validações que antecedem a consulta e a alteração de reembolsos.</summary>
[TestClass]
public sealed class ExpenseServiceValidationOrderTests
{
    private readonly FakeExpenseRepository _repository = new();
    private readonly ExpenseService _service;

    /// <summary>Cria o serviço com dependências isoladas.</summary>
    public ExpenseServiceValidationOrderTests()
    {
        _service = new ExpenseService(_repository, new FixedTimeProvider());
    }

    /// <summary>Usuário sem Approver é recusado antes da busca pelo reembolso a aprovar.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task ApproveAsync_SemRoleApprover_NaoConsultaRepositorio()
    {
        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(
            TestUsers.Employee("ana"),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Forbidden, result.Error?.Type);
        Assert.AreEqual(0, _repository.TransitionLookupCount);
        Assert.AreEqual(0, _repository.SaveCount);
    }

    /// <summary>Usuário sem Approver é recusado antes de validar a justificativa ou buscar o reembolso.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task RejectAsync_SemRoleApproverEJustificativaInvalida_NaoConsultaRepositorio()
    {
        RejectExpenseRequest request = new() { Justification = "curta" };

        ServiceResult<ExpenseResponse> result = await _service.RejectAsync(
            TestUsers.Employee("ana"),
            Guid.NewGuid(),
            request,
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Forbidden, result.Error?.Type);
        Assert.AreEqual(0, _repository.TransitionLookupCount);
        Assert.AreEqual(0, _repository.SaveCount);
    }

    /// <summary>Justificativa inválida é recusada antes da busca pelo reembolso a reprovar.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task RejectAsync_ApproverEJustificativaInvalida_NaoConsultaRepositorio()
    {
        RejectExpenseRequest request = new() { Justification = "curta" };

        ServiceResult<ExpenseResponse> result = await _service.RejectAsync(
            TestUsers.With("bruno", AppRoles.Approver),
            Guid.NewGuid(),
            request,
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Validation, result.Error?.Type);
        Assert.AreEqual(0, _repository.TransitionLookupCount);
        Assert.AreEqual(0, _repository.SaveCount);
    }

    /// <summary>Usuário sem Finance é recusado antes da busca pelo reembolso a pagar.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task PayAsync_SemRoleFinance_NaoConsultaRepositorio()
    {
        ServiceResult<ExpenseResponse> result = await _service.PayAsync(
            TestUsers.With("bruno", AppRoles.Approver),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Forbidden, result.Error?.Type);
        Assert.AreEqual(0, _repository.TransitionLookupCount);
        Assert.AreEqual(0, _repository.SaveCount);
    }
}
