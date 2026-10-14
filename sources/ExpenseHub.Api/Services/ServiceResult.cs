namespace ExpenseHub.Api.Services;

/// <summary>
/// Resultado de uma operação de serviço: um valor em caso de sucesso ou uma <see cref="ServiceError"/>.
/// </summary>
/// <typeparam name="T">Tipo do valor de sucesso.</typeparam>
public sealed class ServiceResult<T>
{
    internal ServiceResult(T? value, ServiceError? error)
    {
        Value = value;
        Error = error;
    }

    /// <summary>Indica se a operação foi concluída.</summary>
    public bool Succeeded => Error is null;

    /// <summary>Valor produzido em caso de sucesso.</summary>
    public T? Value { get; }

    /// <summary>Falha ocorrida, quando houver.</summary>
    public ServiceError? Error { get; }
}

/// <summary>
/// Fábricas de <see cref="ServiceResult{T}"/>.
/// </summary>
public static class ServiceResult
{
    /// <summary>Cria um resultado de sucesso.</summary>
    /// <typeparam name="T">Tipo do valor.</typeparam>
    /// <param name="value">Valor produzido.</param>
    /// <returns>O resultado.</returns>
    public static ServiceResult<T> Success<T>(T value)
    {
        return new ServiceResult<T>(value, null);
    }

    /// <summary>Cria um resultado de falha.</summary>
    /// <typeparam name="T">Tipo do valor esperado em caso de sucesso.</typeparam>
    /// <param name="error">Falha ocorrida.</param>
    /// <returns>O resultado.</returns>
    public static ServiceResult<T> Failure<T>(ServiceError error)
    {
        return new ServiceResult<T>(default, error);
    }
}
