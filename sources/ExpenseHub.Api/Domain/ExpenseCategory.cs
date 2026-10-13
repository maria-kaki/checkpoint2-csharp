namespace ExpenseHub.Api.Domain;

/// <summary>
/// Categoria de despesa (por exemplo, transporte ou hospedagem).
/// </summary>
public class ExpenseCategory
{
    /// <summary>Tamanho máximo do nome da categoria.</summary>
    public const int NameMaxLength = 100;

    /// <summary>
    /// Cria uma categoria.
    /// </summary>
    /// <param name="id">Identificador da categoria.</param>
    /// <param name="name">Nome exibido da categoria.</param>
    public ExpenseCategory(int id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>Identificador da categoria.</summary>
    public int Id { get; private set; }

    /// <summary>Nome exibido da categoria.</summary>
    public string Name { get; private set; }
}
