# api_loja_venda_app

Base de infraestrutura para uma API de loja em ASP.NET Core. O repositório contém serviços de conexão MongoDB, emissão e validação de JWT e hash de senhas. Ainda não há modelos de domínio, controllers ou rotas de negócio.

## Requisitos

- .NET SDK 10
- MongoDB local ou uma connection string fornecida pelo ambiente

## Configuração

O projeto nunca deve receber chaves JWT ou credenciais MongoDB por arquivos versionados. Defina os segredos no ambiente:

```bash
export JWT_SECURITY_KEY="$(openssl rand -hex 48)"
export MONGODB_CONNECTION_STRING="mongodb://localhost:27017/api_loja_venda_app"
dotnet run
```

`JWT_SECURITY_KEY` deve conter pelo menos 48 bytes UTF-8 gerados aleatoriamente. Sem uma chave válida, o serviço falha ao iniciar. `MONGODB_CONNECTION_STRING` substitui o endereço local padrão; credenciais de produção devem ser fornecidas pelo cofre de segredos da implantação.

O emissor, a audiência e os limites de validade ficam na seção `AppSettings` de `appsettings.json`. Eles podem ser substituídos por `AppSettings__Issuer`, `AppSettings__Audience`, `AppSettings__AccessTokenMinutes` e `AppSettings__PasswordResetTokenMinutes`. Em produção, defina também `AllowedHosts` com o domínio público do serviço.

## Serviços disponíveis

- `MongoDbService`: cria um cliente MongoDB singleton.
- `TokenService`: emite tokens de acesso com validade de até 60 minutos e tokens de redefinição com validade de até 120 minutos. Valida assinatura HMAC-SHA384, emissor, audiência, expiração e finalidade do token.
- `HashService`: cria e confere hashes de senha com BCrypt.
- Swagger aparece apenas em Development.

O chamador de `GenerateToken` precisa passar o ID e o e-mail da conta autenticada, obtidos de uma fonte confiável. O repositório ainda não implementa autenticação, controllers, recuperação de senha, revogação ou refresh tokens.

## Desenvolvimento

```bash
dotnet restore
dotnet build --configuration Release
```

O workflow de CI restaura dependências, compila em Release e verifica avisos de vulnerabilidades altas e críticas no NuGet.

## Segurança

Os segredos que já foram versionados precisam ser rotacionados fora deste repositório. A remoção dos arquivos atuais não apaga os valores de commits antigos; a limpeza do histórico Git deve ser coordenada com todos os clones e com o provedor remoto. Consulte [SECURITY.md](SECURITY.md).

## Licença

Distribuído sob a licença MIT. Veja [LICENSE](LICENSE).
