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
├── Responses/                 # contratos de saída
├── Security/                  # roles, regras de alteração de roles e seed
└── Services/                  # regras de negócio e resultado padrão (ServiceResult)
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

Todas as rotas exigem autenticação por padrão (política de fallback); apenas `/health`, `/login` e `/register` são públicas. Após 5 tentativas de login inválidas a conta fica bloqueada por 5 minutos. As respostas de erro seguem `ProblemDetails`.

## Cadastro e administração de usuários

### Cadastro

`POST /register` é público e recebe somente e-mail e senha. O usuário é criado **sem nenhuma role**; enviar qualquer campo extra, como `roles`, retorna `400`.

```json
{ "email": "ana@empresa.com", "password": "Ana@12345" }
```

| Situação | Status |
|---|---|
| Cadastro válido | `201 Created` com `id`, `email` e `roles: []` |
| E-mail já cadastrado, senha fora da política ou campo extra | `400 Bad Request` |

Um usuário recém-cadastrado consegue fazer login, mas recebe `403` em qualquer operação até que o Admin atribua uma role.

### Administração de roles (somente Admin)

| Método e rota | Operação |
|---|---|
| `GET /api/admin/users` | Lista usuários com suas roles |
| `PUT /api/admin/users/{id}/roles` | Substitui as roles do usuário pelo conjunto enviado |

O `PUT` recebe o **conjunto completo** de roles desejado: roles ausentes da lista são removidas e uma lista vazia remove todas.

```json
{ "roles": ["Employee", "Approver"] }
```

| Situação | Status |
|---|---|
| Alteração válida | `200 OK` com as roles finais |
| Lista ausente ou role desconhecida (roles nunca são criadas implicitamente) | `400 Bad Request` |
| Sem token | `401 Unauthorized` |
| Usuário sem a role Admin | `403 Forbidden` |
| Admin removendo a própria role Admin | `403 Forbidden` |
| Usuário inexistente | `404 Not Found` |

Os nomes das roles não diferenciam maiúsculas e minúsculas (`employee` vira `Employee`).

> **Novo login obrigatório após alterar roles.** As roles ficam gravadas no token no momento do login. Depois de uma alteração, o usuário afetado precisa chamar `POST /login` novamente para receber um token com as novas roles; o token anterior continua com as roles antigas até expirar (1 hora). A alteração também renova o security stamp do usuário.

## Reembolsos

### Criar e editar rascunho

| Método e rota | Quem pode | Regra |
|---|---|---|
| `POST /api/expenses` | Employee | Cria em `Draft`; o proprietário é o usuário do token |
| `PUT /api/expenses/{id}` | Employee proprietário | Somente enquanto o reembolso está em `Draft` |

Corpo aceito (o mesmo para criar e editar):

```json
{
  "categoryId": 2,
  "description": "Táxi do aeroporto até o cliente",
  "amount": 87.50,
  "expenseDate": "2026-10-10"
}
```

| Campo | Regra |
|---|---|
| `categoryId` | Obrigatório; categoria existente (1 Alimentação, 2 Transporte, 3 Hospedagem, 4 Material de escritório, 5 Outros) |
| `description` | Obrigatória, entre 10 e 500 caracteres (sem contar espaços nas pontas) |
| `amount` | Entre `0.01` e `2147483647` (R$ 2.147.483.647,00) |
| `expenseDate` | Data válida (`yyyy-MM-dd`) e não futura, comparada com a data UTC do servidor |

Identificador, proprietário, estado, ator e horários são definidos pelo servidor. Enviar qualquer outro campo (por exemplo `ownerId` ou `status`) retorna `400`, o que impede mass assignment.

| Situação | Status |
|---|---|
| Criação válida | `201 Created` com o reembolso em `Draft` |
| Edição válida | `200 OK` com o reembolso atualizado |
| Entrada inválida ou campo extra | `400 Bad Request` |
| Sem token | `401 Unauthorized` |
| Usuário sem a role Employee (inclusive Admin, Approver, Finance e Auditor) | `403 Forbidden` |
| Usuário que enxerga o reembolso, mas não é o proprietário | `403 Forbidden` |
| Reembolso inexistente ou fora do escopo de leitura (por exemplo, de outro Employee) | `404 Not Found` |
| Reembolso fora de `Draft` | `409 Conflict` |

Toda criação e toda edição com mudanças gravam um registro em `ExpenseHistory` na mesma operação, com ator, instante UTC, estados e, na edição, os campos alterados com valores antigos e novos. Uma edição sem mudanças não gera histórico.

### Enviar, listar e consultar

| Método e rota | Quem pode | Regra |
|---|---|---|
| `POST /api/expenses/{id}/submit` | Employee proprietário | `Draft` → `Submitted` |
| `GET /api/expenses` | Employee, Approver, Finance, Auditor | Lista somente o escopo de leitura do perfil |
| `GET /api/expenses/{id}` | Employee, Approver, Finance, Auditor | Detalhe somente dentro do escopo de leitura |

Escopo de leitura por perfil (roles acumuladas recebem a união):

| Perfil | Enxerga |
|---|---|
| Employee | Os próprios reembolsos, em qualquer estado |
| Approver | Reembolsos `Submitted` |
| Finance | Reembolsos `Approved` e `Paid` |
| Auditor | Todos |
| Admin (sem outra role) | Nada (`403`) |

| Situação | Status |
|---|---|
| Envio válido | `200 OK` com o reembolso em `Submitted` |
| Envio repetido ou reembolso fora de `Draft` | `409 Conflict`, sem novo registro de histórico |
| Envio por quem enxerga o reembolso, mas não é o proprietário | `403 Forbidden` |
| Envio por Auditor, Approver, Finance ou Admin sem a role Employee | `403 Forbidden` |
| Reembolso inexistente ou fora do escopo de leitura | `404 Not Found` (a mesma resposta, sem revelar que o recurso existe) |
| Listagem ou detalhe por usuário sem perfil de leitura (por exemplo, só Admin) | `403 Forbidden` |
| Sem token | `401 Unauthorized` |

O envio grava o histórico (`Submitted`, ator, instante UTC, `Draft` → `Submitted`) na mesma operação. A listagem é ordenada do mais recente para o mais antigo e o filtro de perfil é executado no banco.

### Onde as regras ficam

- **Controller:** `[Authorize(Roles = ...)]` restringe a rota pela role.
- **Serviço (`ExpenseService`):** combina role, visibilidade, proprietário e estado, nessa ordem, e devolve um `ServiceResult` que o controller converte em `ProblemDetails`.
- **Visibilidade (`ExpenseVisibility`):** filtro aplicado na consulta ao banco, antes de materializar os dados. Employee vê os próprios, Approver vê `Submitted`, Finance vê `Approved` e `Paid`, Auditor vê todos e Admin sozinho não vê nada. Roles acumuladas recebem a união.
- **Domínio (`Expense`, `ExpenseRules`):** garante o estado e as validações mesmo que o serviço seja contornado.

## Testes

Os testes unitários ficam em `sources/ExpenseHub.UnitTests` (MSTest) e executam sem banco, rede ou serviço externo: os serviços dependem de `IExpenseRepository` e `TimeProvider`, substituídos nos testes por um repositório em memória e um relógio fixo.

```shell
dotnet test ./sources/ExpenseHub.slnx
```

## Entrega

Entregue:

- URL do repositório público;
- commit Secure Hash Algorithm (SHA) final;
- integração contínua executada;
- documentação atualizada.

O projeto deve compilar sem erros e ser entregue sem warnings para receber a pontuação integral de Qualidade de Código.
