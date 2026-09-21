# api_loja_venda_app

## 🐳 Instalação e Execução (Docker) — recomendado

### Pré-requisitos
- [Docker](https://docs.docker.com/get-docker/) + Docker Compose

### Rodar com Docker
```bash
docker compose up --build
```
```bash
docker run --rm -v $(pwd):/src -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet run
```

### Sem Docker (local)
```bash
# Requer .NET SDK
dotnet build
dotnet run
```

Base de uma API de loja/vendas em ASP.NET Core 5: infraestrutura de conexão com MongoDB, geração e validação de tokens JWT e hash de senhas com BCrypt, documentada via Swagger.

![C#](https://img.shields.io/badge/C%23-239120?style=flat-square&logo=csharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-5.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![MongoDB](https://img.shields.io/badge/MongoDB-47A248?style=flat-square&logo=mongodb&logoColor=white)
![JWT](https://img.shields.io/badge/JWT-black?style=flat-square&logo=jsonwebtokens)
![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)
![Status](https://img.shields.io/badge/status-em%20desenvolvimento-yellow?style=flat-square)

## Sobre

Projeto de API para um aplicativo de loja/vendas (`loj.app`). O repositório reúne a camada de infraestrutura reutilizável do backend — acesso ao MongoDB, autenticação por JWT e hashing de senhas — já ligada por injeção de dependência e exposta no Swagger. As regras de negócio e os controllers da loja ainda não estão implementados neste repositório.

## Funcionalidades

Comprovadas pelo código em `Services/Default/` e `DependencyServices/`:

- **Conexão MongoDB** via `MongoDbService`, registrado como singleton e configurado pela seção `DatabaseSetting` do `appsettings.json`.
- **Tokens JWT** com `TokenService` (`System.IdentityModel.Tokens.Jwt`, assinatura HMAC-SHA384):
  - token de acesso com 30 dias de validade;
  - token de redefinição de senha com 2 horas de validade e claim de e-mail;
  - validação de token com checagem de expiração e leitura do payload (`JwtPayload`).
- **Hash de senha** com `HashService` (BCrypt.Net-Next, variante SHA-512) para criar hash e verificar credenciais.
- **Swagger UI** com comentários XML habilitados (`GenerateDocumentationFile` no `.csproj`), acessível em `/swagger` no ambiente de desenvolvimento.

> Não há controllers, models de domínio ou rotas de negócio neste repositório.

## Stack

- **Linguagem/framework**: C# com ASP.NET Core 5.0 (Web API)
- **Banco de dados**: MongoDB — `MongoDB.Bson`, `MongoDB.Driver`, `MongoDB.Driver.Core` 2.13.2
- **Autenticação**: `System.IdentityModel.Tokens.Jwt` 6.13.1
- **Hash de senhas**: `BCrypt.Net-Next` 4.0.2
- **Serialização**: `Newtonsoft.Json` 13.0.1
- **Documentação**: `Swashbuckle.AspNetCore` 5.6.3
- **E-mail**: SMTP (Zoho Mail)

## Como rodar

Requisito documentado originalmente: **.NET Core 5 — versão 5.0.302**.

```bash
dotnet restore
dotnet run
```

Configuração necessária no `appsettings.json` (seções lidas por `Startup.cs`):

```json
{
  "DatabaseSetting": { "ConnectionString": "mongodb://usuario:senha@host:27017" },
  "AppSettings": {
    "JWT_SecurityKey": "<chave-de-assinatura>",
    "JWT_Issuer": "",
    "JWT_Audience": ""
  }
}
```

> Atenção: a chave `JWT_SecurityKey` e a connection string atualmente versionadas são de desenvolvimento e devem ser substituídas por variáveis de ambiente/segredos antes de qualquer uso real.

SMTP utilizado pelo projeto (documentado no README original): servidor `smtp.zoho.com`, porta `465`, segurança `SSL`, remetente `contato@loj.app`. A senha do e-mail **não** é versionada e deve ser fornecida via configuração/segredo.

## Estrutura do projeto

```
api_loja_venda_app/
├── DependencyServices/         # Interfaces: IMongoDbService, ITokenService
├── Models/
│   └── Configs/                # AppSettings (JWT) e DatabaseSetting (Mongo)
├── Services/
│   └── Default/                # HashService, MongoDbService, TokenService
├── Properties/                 # launchSettings.json
├── Program.cs
├── Startup.cs                  # DI, Swagger e pipeline HTTP
└── api_loja_venda_app.csproj
```

## Licença

Distribuído sob a licença MIT. Veja [LICENSE](LICENSE).
