using System.Collections.Generic;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Tipos de falha de uma operação de serviço. Cada tipo corresponde a um status HTTP do contrato.
/// </summary>
public enum ServiceErrorType
{
    /// <summary>Entrada inválida (400).</summary>
    Validation,

    /// <summary>Recurso inexistente ou fora do escopo de leitura (404).</summary>
    NotFound,

    /// <summary>Usuário autenticado sem permissão para a operação (403).</summary>
    Forbidden,

    /// <summary>Transição incompatível ou repetida (409).</summary>
    Conflict,
}

/// <summary>
/// Falha de uma operação de serviço.
/// </summary>
/// <param name="Type">Tipo da falha.</param>
/// <param name="Message">Mensagem para o cliente.</param>
/// <param name="Errors">Erros de validação por campo, quando houver.</param>
public sealed record ServiceError(
    ServiceErrorType Type,
    string Message,
    IReadOnlyDictionary<string, string[]>? Errors = null)
{
    /// <summary>Cria uma falha de validação para um campo.</summary>
    /// <param name="field">Campo inválido.</param>
    /// <param name="message">Descrição do problema.</param>
    /// <returns>A falha.</returns>
    public static ServiceError Validation(string field, string message)
    {
        return new ServiceError(
            ServiceErrorType.Validation,
            message,
            new Dictionary<string, string[]> { [field] = [message] });
    }

    /// <summary>Cria uma falha de recurso inexistente ou invisível.</summary>
    /// <param name="message">Descrição do problema.</param>
    /// <returns>A falha.</returns>
    public static ServiceError NotFound(string message)
    {
        return new ServiceError(ServiceErrorType.NotFound, message);
    }

    /// <summary>Cria uma falha de permissão.</summary>
    /// <param name="message">Descrição do problema.</param>
    /// <returns>A falha.</returns>
    public static ServiceError Forbidden(string message)
    {
        return new ServiceError(ServiceErrorType.Forbidden, message);
    }

    /// <summary>Cria uma falha de conflito de estado.</summary>
    /// <param name="message">Descrição do problema.</param>
    /// <returns>A falha.</returns>
    public static ServiceError Conflict(string message)
    {
        return new ServiceError(ServiceErrorType.Conflict, message);
    }
}
