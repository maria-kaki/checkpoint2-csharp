# Checkpoint 2 — ExpenseHub

Checkpoint de C# em grupos de até 3 pessoas para construção de uma Application Programming Interface (API) corporativa de reembolsos.

O prazo de entrega é **13 de outubro de 2026**. O grupo deverá implementar autenticação, autorização, fluxo de aprovação e reprovação, pagamento simulado, histórico e testes unitários.

## Criar seu repositório

1. Clique em **Use this template**.
2. Selecione **Create a new repository**.
3. Crie um repositório **público** em uma das contas do grupo.
4. Adicione os demais integrantes como colaboradores.
5. Clone o repositório.

Não use fork. As issues permanecem neste repositório original como especificação comum da turma.

Os commits serão utilizados para avaliar a participação. Todos os membros do grupo
devem possuir mais de um commit no repositório.

## Fluxo de trabalho

Para cada issue:

1. leia os critérios no repositório original;
2. crie uma branch com o identificador, por exemplo `i06-ownership`;
3. implemente e valide a feature;
4. abra uma pull request no seu próprio repositório;
5. use um título como `I06 — Ownership e matriz de acesso`;
6. adicione na descrição uma referência completa, como `Racass/checkpoint-csharpracass-expensehub#6`;
7. não use `Closes`, `Fixes` ou `Resolves`, pois a issue original deve permanecer aberta;
8. conclua a auto-revisão e faça o merge.

## Estrutura inicial

```text
sources/
├── ExpenseHub.slnx
├── ExpenseHub.Api/
└── ExpenseHub.UnitTests/
```

```text
sources/ExpenseHub.Api/
├── Controllers/               # endpoints HTTP
├── Data/
│   ├── AppDbContext.cs        # contexto EF Core + Identity
│   ├── IdentitySeeder.cs      # roles obrigatórias e conta Admin inicial
│   ├── Configurations/        # mapeamentos das entidades
│   └── Migrations/            # migrations geradas pelo dotnet-ef
├── Domain/                    # Expense, ExpenseCategory, ExpenseHistory, PaymentRecord
├── Dtos/                      # contratos de entrada com validação declarativa
└── Security/                  # roles e configuração do seed
```

## Comandos

```shell
dotnet tool restore
dotnet restore ./sources/ExpenseHub.slnx
dotnet build ./sources/ExpenseHub.slnx
dotnet test ./sources/ExpenseHub.slnx

# uma única vez: senha da conta Admin inicial (veja "Autenticação e conta Admin")
dotnet user-secrets set "Seed:Admin:Password" "<sua-senha-forte>" --project ./sources/ExpenseHub.Api

dotnet run --project ./sources/ExpenseHub.Api/ExpenseHub.Api.csproj
```

A API sobe em `http://localhost:5245`. O endpoint `GET /health` é público e confirma que a aplicação iniciou. O arquivo [ExpenseHub.Api.http](sources/ExpenseHub.Api/ExpenseHub.Api.http) contém as requisições usadas para validar cada issue.

## Documentação

- [Enunciado](docs/ENUNCIADO.md)
- [Requisitos e contratos](docs/REQUISITOS.md)
- [Rubrica](docs/RUBRICA.md)
- [Matriz de autorização](docs/MATRIZ-AUTORIZACAO.md)
- [Processo no GitHub](docs/PROCESSO-GITHUB.md)
- [Uso de Inteligência Artificial](docs/USO-DE-IA.md)
- [Regras do pipeline de qualidade](docs/code-quality-rules.md)

## Banco de dados

O projeto usa **SQLite** com Entity Framework Core 10.

| Item | Valor |
|---|---|
| Provider | SQLite |
| Pacote | `Microsoft.EntityFrameworkCore.Sqlite` 10.0.12 |
| Connection string | `ConnectionStrings:ExpenseHub` em `appsettings.json` (`Data Source=expensehub.db`) |
| Arquivo gerado | `sources/ExpenseHub.Api/expensehub.db` (ignorado pelo Git) |
| Migrations | `sources/ExpenseHub.Api/Data/Migrations` |

Não é necessário instalar servidor de banco. Ao iniciar, a API aplica automaticamente as migrations pendentes e cria o arquivo `expensehub.db` caso ele não exista.

Para criar ou atualizar o banco manualmente, use a ferramenta local `dotnet-ef` (registrada em `dotnet-tools.json`):

```shell
dotnet tool restore
dotnet ef database update --project ./sources/ExpenseHub.Api
```

Para criar uma nova migration após alterar o modelo:

```shell
dotnet ef migrations add NomeDaMigration --project ./sources/ExpenseHub.Api --output-dir Data/Migrations
```

Para recriar o banco do zero, pare a aplicação e apague `sources/ExpenseHub.Api/expensehub.db`.

O banco inicia com as categorias de despesa: Alimentação, Transporte, Hospedagem, Material de escritório e Outros.

## Autenticação e conta Admin

A autenticação usa **ASP.NET Core Identity** persistido no SQLite e **bearer token** do próprio Identity (`AddBearerToken`).

### Conta Admin inicial

Ao iniciar, a API executa um seed idempotente que:

- cria as roles `Admin`, `Employee`, `Approver`, `Finance` e `Auditor`, caso não existam;
- cria **somente uma** conta Admin, caso ainda não exista nenhum usuário com a role `Admin`.

Reexecutar a aplicação não duplica roles nem usuários. Nenhum outro usuário é criado pelo seed.

| Configuração | Onde fica |
|---|---|
| `Seed:Admin:Email` | `appsettings.json` (padrão `admin@expensehub.local`) |
| `Seed:Admin:Password` | **User Secrets** ou variável de ambiente; nunca no repositório |

Configure a senha antes da primeira execução:

```shell
dotnet user-secrets set "Seed:Admin:Password" "<sua-senha-forte>" --project ./sources/ExpenseHub.Api
```

Ou, sem User Secrets (por exemplo fora do ambiente Development), use a variável de ambiente `Seed__Admin__Password`.

A senha deve seguir a política padrão do Identity: ao menos 6 caracteres, com letra maiúscula, letra minúscula, dígito e caractere especial. Se nenhuma conta Admin existir e a senha não estiver configurada, a API não inicia e informa o que falta.

### Login

`POST /login` recebe e-mail e senha e devolve um bearer token válido por 1 hora:

```http
POST http://localhost:5245/login
Content-Type: application/json

{ "email": "admin@expensehub.local", "password": "<sua-senha-forte>" }
```

```json
{ "tokenType": "Bearer", "accessToken": "...", "expiresIn": 3600, "refreshToken": "..." }
```

Envie o token nas demais rotas com o cabeçalho `Authorization: Bearer <accessToken>`. As roles do usuário são gravadas no token no momento do login.

### Respostas de autenticação

| Situação | Status |
|---|---|
| Credenciais inválidas no login | `401 Unauthorized` |
| Rota protegida sem token ou com token inválido | `401 Unauthorized` |
| Usuário autenticado sem a role exigida | `403 Forbidden` |
| Entrada inválida | `400 Bad Request` |

Todas as rotas exigem autenticação por padrão (política de fallback); apenas `/health` e `/login` são públicas. Após 5 tentativas de login inválidas a conta fica bloqueada por 5 minutos. As respostas de erro seguem `ProblemDetails`.

## Testes

Somente testes unitários escritos por você entram na nota. Testes de integração, end-to-end ou de interface são permitidos, mas opcionais e sem pontuação.

Os testes unitários devem executar sem banco, rede ou serviço externo.

## Entrega

Entregue:

- URL do repositório público;
- commit Secure Hash Algorithm (SHA) final;
- integração contínua executada;
- documentação atualizada.

O projeto deve compilar sem erros e ser entregue sem warnings para receber a pontuação integral de Qualidade de Código.
