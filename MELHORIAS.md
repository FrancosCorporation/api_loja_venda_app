# MELHORIAS — api_loja_venda_app

> **Gerado por análise de código em 2026-10-02** · Stack: .NET 5 (ASP.NET Core, JWT HMAC-SHA384, MongoDB, BCrypt SHA-512)
> Branch `heads/origin` · base `0adf2bf` · ~342 LOC · **0 testes** · sem CI
>
> **Este arquivo é um plano de execução.** Cada item tem ID, `arquivo:linha`, mudança exata,
> critério de aceite e comando de verificação.

---

## 0. Como usar este documento

1. Execute na ordem **P0 → P1 → P2 → P3**, respeitando as ondas da §8.
2. Ao terminar um item: marque `- [x]`, rode o **Verificação**, comite `fix(<ID>): descrição`.
3. **Não copie o padrão de `ValidateToken` da api_authentication** — este projeto tem um defeito
   **mais grave** e **diferente**: o claim `id` é literalmente `"1"` para **todos** os tokens
   (`TokenService.cs:36`). Ver `SEC-03`.
4. **A branch é `heads/origin`** (não `main`/`master`) — verifique o fluxo de push antes.
5. **Idioma:** português.

---

## 1. Diagnóstico executivo

API de loja: token JWT, hash de senha com BCrypt/SHA-512 e acesso a MongoDB. A estrutura de pasta
está correta (Controllers/Services/Models/DependencyServices) mas **não há controller nenhum** — só os
serviços.

**O que está bem (não reaça):**

| Item | Evidência |
|---|---|
| Senha com **BCrypt** (SHA-512 comoalgoritmo) | `HashService.cs:8,19` |
| Token de reset de senha com **2 h** de validade | `TokenService.cs:62` (curto e adequado) |
| HMAC-SHA384 (mais forte que HS256) | `TokenService.cs:39,63` |
| `Encoding.UTF8.GetBytes` (não `ASCII`, que trunca) | `TokenService.cs:28,52,73` |
| `ValidateToken` retorna `bool`, não expõe claims | `TokenService.cs:98` |
| Dependência em `IConfiguration` (injetável/testável) | `TokenService.cs:20-22` |
| Swagger só em Development | `Startup.cs:56-61` |

**O que está quebrado:**

1. **`JWT_SecurityKey` com valor real** em `appsettings.json` **versionado** — publicado no GitHub.
2. **Claim `id` hardcoded `"1"`** (`TokenService.cs:36`) — **todos** os tokens dizem ser o usuário 1.
3. **Token de login válido por 30 dias** (`TokenService.cs:38`) — e o reset é 2 h. Inconsistente.
4. **`ValidateAudience`/`ValidateIssuer` = false**, com `Issuer`/`Audience` **vazios** no appsettings.

---

## 2. Tabela de prioridades

| ID | Título | Sev | Arquivo | Depende de |
|---|---|---|---|---|
| SEC-01 | `JWT_SecurityKey` real em `appsettings.json` versionado | **P0** | `appsettings.json:6` | — |
| SEC-02 | Token de login com **30 dias** de validade | **P0** | `TokenService.cs:38` | — |
| SEC-03 | Claim `id` hardcoded `"1"` — todos são o usuário 1 | **P0** | `TokenService.cs:36` | — |
| SEC-04 | `Audience`/`Issuer` vazios e não validados | **P1** | `TokenService.cs:80-83` | SEC-01 |
| SEC-05 | Sem `Issuer`/`Audience` no token gerado | **P1** | `TokenService.cs:34-38` | SEC-04 |
| SEC-06 | `ValidateToken` com `Split` implícito e sem try/catch de header | **P1** | `TokenService.cs:69` | — |
| SEC-07 | `AllowedHosts: "*"` | **P2** | `appsettings.json:12` | — |
| BUG-01 | `LifetimeValidator` customizado sobrepõe `ValidateLifetime` | **P2** | `TokenService.cs:79,85-92` | — |
| BUG-02 | `GenerateToken()` sem parâmetro — não sabe **quem** é | **P0** | `TokenService.cs:24` | SEC-03 |
| BUG-03 | `catch (SecurityTokenException e)` só loga em `Console.Write` | **P2** | `TokenService.cs:100-103` | — |
| IMP-01 | Sem controller: a API não expõe nada | **P1** | *(ausente)* | — |
| IMP-02 | Chave lida e desserializada a cada chamada | **P2** | `TokenService.cs:26-28` | — |
| TEST-01 | Zero testes | **P1** | *(ausente)* | SEC-02, SEC-03 |
| DEVOPS-01 | .NET 5 EOL → .NET 8 + CVEs | **P1** | `api_loja_venda_app.csproj:4` | — |
| DEVOPS-02 | `obj/`/`bin/` versionados | **P2** | `.gitignore` | — |
| DEVOPS-03 | Sem CI | **P2** | *(ausente)* | — |
| DEVOPS-04 | Branch `heads/origin` (fluxo de push não padrão) | **P2** | git | — |
| DOC-01 | README não documenta variáveis nem rotação da chave | **P2** | `README.md` | SEC-01 |
| DOC-02 | Falta `SECURITY.md` | **P3** | *(ausente)* | — |

**Placar: 4 P0 · 6 P1 · 6 P2 · 1 P3 = 17 itens.**

---

## 3. Segurança

### SEC-01 · `JWT_SecurityKey` real em `appsettings.json` versionado · [P0]

- **Arquivo:** `appsettings.json:6`
- **Evidência:** o arquivo **está no índice do git** (`git ls-files` → `appsettings.json`) e contém
  `"AppSettings": { "JWT_SecurityKey": "<valor real>", ... }`. Verifiquei mascarando o valor.
- **Impacto:** a chave que **assina e valida todos os JWTs** está publicada no GitHub. Com ela, um
  atacante assina token com qualquer claim — inclusive `role`/`id` que a aplicação leia. Como
  `SEC-03` mostra que o `id` é fixo, o dano é ainda maior: basta um token válido para impersonar.
  Também quebra o `SEC-04` (não há `aud`/`iss` para restringir).
- **Mudança:** (1) `appsettings.json` versionado **sem** segredo — remover a linha
  `JWT_SecurityKey`; (2) `appsettings.Development.json` com placeholder (ou nada);
  (3) adicionar ao `.gitignore`: `appsettings.Production.json` (o padrão do ASP.NET);
  (4) ler de env/secret store: `Environment.GetEnvironmentVariable("JWT_SECURITY_KEY")`;
  (5) **rotacionar** a chave exposta.
- **Aceite:** o arquivo versionado não tem segredo; sem a variável a API falha alto.
- **Verificação:**
  ```bash
  grep -n 'JWT_SecurityKey' appsettings.json   # deve sumir do arquivo versionado
  git log -S'JWT_SecurityKey' --oneline        # historico tem o valor -> rotacionar
  ```

### SEC-02 · Token de login com **30 dias** · [P0]

- **Arquivo:** `TokenService.cs:38` (`Expires = DateTime.UtcNow.AddDays(30)`)
- **Evidência:** 30 dias, enquanto o token de reset de senha (`TokenService.cs:62`) dura 2 h — o mesmo
  arquivo, a mesma chave, duas políticas sem justificativa.
- **Impacto:** janela de exposição de **30 dias** para um token roubado. Sem `jti` nem revogação
  (não há blacklist), não há como invalidar: o usuário teria que trocar a chave inteira. Se o token
  vazar (log, `Referer`, URL), o atacante tem um mês de acesso.
- **Mudança:** (1) reduzir para **1 h** (ou 15 min, alinhando com o padrão dos projetos novos —
  `agendaflow-saas` usa `24h`, `kanbanex` `24h`; **1 h** é o melhor para login);
  (2) implementar **refresh token** com rotação; (3) adicionar `jti` + revogação (`SEC-03`);
  (4) alinhar com o `2 h` do reset — não faz sentido o login dure 15× o reset.
- **Aceite:** token de login expira em **1 h**; refresh permite sessão longa sem token longo.
- **Verificação:**
  ```bash
  # decodificar exp-iat do token emitido -> deve ser <= 3600
  ```

### SEC-03 · Claim `id` hardcoded `"1"` — todos são o usuário 1 · [P0]

- **Arquivo:** `TokenService.cs:34-37`
- **Evidência:**
  ```csharp
  Subject = new ClaimsIdentity(new Claim[]
  {
      new Claim("id", "1"),
  }),
  ```
  O `id` é a **string literal `"1"`**, e `GenerateToken()` (linha 24) **não recebe parâmetro nenhum** —
  não tem como saber quem está autenticando.
- **Impacto:** **todos** os tokens emitidos são do "usuário 1". Se a aplicação usa esse claim para
  consultar dados (pedidos, carrinho, endereço), **todo mundo vê a conta do usuário 1** — e o dono
  da conta 1 vê os pedidos de todos que se cadastraram. É IDOR **no nível do token**, não da rota.
  É o defeito mais grave deste repositório e não tem correção parcial: enquanto o `GenerateToken`
  não receber o usuário, a API não é usável com segurança.
- **Mudança:** (1) `GenerateToken(UserModel user)` (ou `id`, `email`, `role`) — assinatura da interface
  `ITokenService` junto; (2) claims reais: `ClaimTypes.NameIdentifier`, `ClaimTypes.Email`,
  `ClaimTypes.Role`; (3) **nunca** hardcodar identidade no token.
- **Aceite:** dois usuários diferentes recebem tokens com `sub` distintos; o `id` não é literal.
- **Verificação:**
  ```bash
  # logar A e B, decodificar os tokens -> sub diferentes
  # e o id NUNCA e a string "1"
  ```

### SEC-04 · `Audience`/`Issuer` vazios e não validados · [P1]

- **Arquivo:** `TokenService.cs:80-83` · `appsettings.json:7-8`
- **Evidência:** `ValidateAudience = false`, `ValidateIssuer = false`, e no appsettings
  `"JWT_Issuer": ""`, `"JWT_Audience": ""` (vazios). Os comentários dizem "Because there is no
  audiance in the generated token".
- **Impacto:** qualquer serviço que use a **mesma chave** e o mesmo formato emite token aceito aqui
  (*token confusion*). Como a chave está publicada (`SEC-01`), isso é pior: **qualquer pessoa**
  emite token válido.
- **Mudança:** (1) definir `Issuer`/`Audience` no appsettings/env; (2) `ValidateIssuer/Audience = true`
  com os valores; (3) incluir `Issuer`/`Audience` no `SecurityTokenDescriptor` (`SEC-05`).
- **Aceite:** token assinado com a chave mas **sem** `iss`/`aud` corretos → rejeitado.
- **Verificação:** token forjado sem `iss` → `ValidateToken` devolve `false`.

### SEC-05 · `Issuer`/`Audience` ausentes no token gerado · [P1]

- **Arquivo:** `TokenService.cs:32-40` e `:57-64`
- **Evidência:** o `SecurityTokenDescriptor` define `Subject`, `Expires` e `SigningCredentials` —
  **sem** `Issuer`/`Audience` (embora os campos existam no appsettings, são ignorados).
- **Impacto:** é o par de `SEC-04` — não adianta validar `aud` se o token nunca o emite. Ainda,
  habilita o `SEC-04` (adicionar as claims).
- **Mudança:** `Issuer = appSettings.JWT_Issuer, Audience = appSettings.JWT_Audience` nos **dois**
  geradores (login e reset).
- **Aceite:** o token emitido contém `iss` e `aud` com os valores configurados.
- **Verificação:**
  ```bash
  # decodificar payload -> iss e aud presentes
  ```

### SEC-06 · `ValidateToken(string)` recebe só o token, sem extrair o header · [P1]

- **Arquivo:** `TokenService.cs:69`
- **Evidência:** `ValidateToken(string jwtString)` recebe **apenas a string** — quem chama tem que
  extrair o header antes. Não há `TryExtractBearer` no projeto (ao contrário de `kanbanex`, que tem —
  ver `auth.js:35`).
- **Impacto:** cada chamador vai repetir a extração; é o padrão que gera o bug `Split[1]` visto nos
  projetos irmãos (`api_authentication`). Não é explorável sozinho, mas é o caminho para isso.
- **Mudança:** adicionar `ValidateRequest(HttpRequest)` que extrai o header com validação (igual ao
  `SEC-04` do `api_authentication`) e usar nas rotas.
- **Aceite:** requisição sem header → `401`, sem exceção.
- **Verificação:** `curl -i <rota protegida>` sem header → `401` (não `500`).

---

## 4. Bugs e defeitos funcionais

### BUG-02 · `GenerateToken()` sem parâmetro — não sabe **quem** é · [P0]

- **Arquivo:** `TokenService.cs:24` · interface `DependencyServices/ITokenService.cs`
- **Evidência:** `public dynamic GenerateToken()` — **zero parâmetros**, e emite `id = "1"`
  (literal). É a causa raiz do `SEC-03`.
- **Impacto:** a API não tem como emitir token específico. Enquanto isso, o usuário 1 é o "dono"
  de todos. **Não há correção parcial**: sem mudar a assinatura, qualquer controller que chamar
  `GenerateToken()` continua emitindo o mesmo token.
- **Mudança:** ver `SEC-03` — mudar a assinatura para receber o usuário e as claims dele. Atualizar
  `ITokenService` e todos os chamadores.
- **Aceite:** a assinatura recebe o usuário; nenhum literal de identidade no código.
- **Verificação:** `grep -n 'GenerateToken()' Services/ DependencyServices/` → nenhuma ocorrência
  sem parâmetro.

### BUG-01 · `LifetimeValidator` customizado sobrepõe `ValidateLifetime` · [P2]

- **Arquivo:** `TokenService.cs:79` e `:85-92`
- **Evidência:** `ValidateLifetime = true` **e** um `LifetimeValidator` customizado — o segundo
  **substitui** o primeiro. O customizado só checa `expires` (`DateTime.Now < expires`), ignorando
  `notBefore`.
- **Impacto:** (a) redundância — dois mecanismos para a mesma coisa; (b) o customizado é **menos**
  rigoroso (não valida `notBefore`, e usa `DateTime.Now` local em vez de `UtcNow` — problema de fuso);
  (c) o comentário na linha 79 diz "Because there is no expiration in the generated token", que é
  **falso** (há `Expires` na linha 38).
- **Mudança:** (1) remover o `LifetimeValidator` e deixar o `ValidateLifetime` padrão (valida `exp` e
  `nbf` corretamente, em UTC); (2) corrigir o comentário enganoso.
- **Aceite:** token expirado ou "do futuro" → `false` (com validação padrão e UTC).
- **Verificação:**
  ```bash
  grep -n 'LifetimeValidator' Services/Default/TokenService.cs && echo 'ainda customizado' || echo OK
  ```

### BUG-03 · Erro de token só vai para `Console.Write` · [P2]

- **Arquivo:** `TokenService.cs:100-103`
- **Evidência:** `catch (SecurityTokenException e) { Console.Write(e.Message); return false; }` — sem
  `ILogger`, sem nível, e a mensagem vai para stdout solta.
- **Impacto:** em produção, erro de validação de token (que pode ser ataque) **não fica em log
  estruturado** — impossível correlacionar/alerta. E `Console.Write` em container vai para stdout sem
  contexto (sem request id, sem endpoint).
- **Mudança:** (1) injetar `ILogger<TokenService>`; (2) `logger.LogWarning(ex, "token invalido")` (é
  sinal de possível ataque); (3) nunca retornar a mensagem ao cliente.
- **Aceite:** tentativa de token inválido aparece no log estruturado.
- **Verificação:**
  ```bash
  grep -n 'Console.Write' Services/Default/TokenService.cs && echo 'ainda console' || echo OK
  ```

---

## 5. Qualidade: testes

### IMP-01 · Sem controller: a API não expõe nada · [P1]

- **Arquivo:** *(ausente)* — `find . -name '*Controller*'` → **nenhum** (fora de `obj/`/`bin/`).
- **Evidência:** existem `Services/`, `Models/`, `DependencyServices/` e `Startup.cs`, mas **nenhum
  controller**. O `Startup.cs:71` faz `endpoints.MapControllers()` sem nada para mapear.
- **Impacto:** a API **não serve nenhuma rota**. Isso é mais grave do que parece: o `SEC-03` (token do
  usuário 1) só importa quando houver rota que use o token — e ela **vai existir**. Ou seja: o
  trabalho de segurança precisa ser feito **antes** de expor a API, não depois.
- **Mudança:** (1) **primeiro** os itens `SEC-01`..`SEC-03` (token correto); (2) só então criar
  controllers (`AuthController` com login/reset), com `[Authorize]` e `exigirToken` no `Startup`.
- **Aceite:** token emitido tem o `id` do usuário correto **antes** de qualquer rota existir.
- **Verificação:**
  ```bash
  find . -name '*Controller.cs' -not -path '*/obj/*' | wc -l   # quando criado
  ```

### IMP-02 · Chave lida e desserializada a cada chamada · [P2]

- **Arquivo:** `TokenService.cs:26-28` (repetido em `:50-52` e `:71-73`)
- **Evidência:** em **cada** chamada de `GenerateToken`/`ValidateToken`, o código faz
  `GetSection(nameof(AppSettings)).Get<AppSettings>()` — relê e desserializa a configuração.
- **Impacto:** pequeno isoladamente, mas em `ValidateToken` (chamado por requisição) é trabalho
  repetido. E mais importante: **não valida** que a chave tem tamanho mínimo — `Encoding.UTF8.GetBytes`
  de uma chave curta/ausente gera chave HMAC fraca (ou `null` → exceção).
- **Mudança:** (1) injetar a configuração **uma vez** (via `IOptions<AppSettings>` ou singleton);
  (2) **validar** no boot: chave presente e ≥ 32 bytes, senão falhar alto.
- **Aceite:** sem `JWT_SecurityKey` válida, a API não sobe.
- **Verificação:**
  ```bash
  unset JWT_SECURITY_KEY; dotnet run   # deve falhar com mensagem
  ```

### TEST-01 · Zero testes · [P1]

- **Arquivo:** *(ausente)*
- **Evidência:** nenhum `*Tests*.csproj`, nenhum `[Fact]`. Projeto com JWT e hash de senha sem
  um único teste.
- **Impacto:** os 4 P0 (`SEC-01` chave, `SEC-02` 30 dias, `SEC-03` id "1", `BUG-02` assinatura)
  **não têm** como ser detectados por teste. É o mesmo padrão dos outros projetos .NET da raiz.
- **Mudança:** criar `api_loja_venda_app.Tests` (xUnit) com: (1) token do usuário A tem `sub` = A e
  **não** "1"; (2) `exp - iat` ≤ 3600; (3) token expirado → `false`; (4) token sem `aud` → `false`;
  (5) `HashService` roundtrip + senha errada lança.
- **Aceite:** ≥ 5 testes; o do `sub` falha se alguém reintroduzir `"1"`.
- **Verificação:**
  ```bash
  dotnet test    # >= 5 testes, 0 falhas
  ```

---

## 6. DevOps / Infra

### DEVOPS-01 · .NET 5 EOL + pacotes antigos · [P1]

- **Arquivo:** `api_loja_venda_app.csproj:4` e dependências
- **Evidência:** `<TargetFramework>net5.0</TargetFramework>` (EOL desde 10/05/2022) e pacotes
  `MongoDB.Driver 2.13.2`, `System.IdentityModel.Tokens.Jwt 6.13.1`, `Swashbuckle 5.6.3`,
  `BCrypt.Net-Next 4.0.2`.
- **Impacto:** runtime sem patch de segurança; `System.IdentityModel.Tokens.Jwt 6.13.1` é o que
  **valida o token** — CVE nesse pacote é CVE de autenticação.
- **Mudança:** (1) `net8.0` (LTS); (2) alinhar os pacotes (`System.IdentityModel.Tokens.Jwt 8.x`,
  MongoDB.Driver atual); (3) `dotnet list package --vulnerable --include-transitive` vazio;
  (4) remover `MongoDB.Driver.Core` (obsoleto/duplicado).
- **Aceite:** compila em net8.0 e `--vulnerable` vazio.
- **Verificação:**
  ```bash
  sed -i 's|net5.0|net8.0|' api_loja_venda_app.csproj
  dotnet restore && dotnet build && dotnet test
  dotnet list package --vulnerable --include-transitive
  ```

### DEVOPS-02 · `obj/`/`bin/` versionados · [P2]

- **Arquivo:** `obj/Debug/net5.0/*.cs`, `bin/Debug/net5.0/*` (visíveis no inventário)
- **Evidência:** artefatos de build no índice.
- **Impacto:** ruído no histórico; `.csproj` de outra versão do .NET pode ser confundido.
- **Mudança:** `git rm -r --cached obj bin` + garantir no `.gitignore`.
- **Aceite:** `git ls-files | grep -c '^obj/'` → 0.
- **Verificação:**
  ```bash
  git rm -r --cached obj bin 2>/dev/null; git ls-files | grep -c '^obj/'
  ```

### DEVOPS-03 · Sem CI · [P2]

- **Arquivo:** *(ausente)* `.github/workflows/`
- **Evidência:** sem workflow.
- **Impacto:** nada trava os P0 de volta — em especial o `id = "1"`, que é uma linha.
- **Mudança:** `ci.yml`: `setup-dotnet` (net8.0), `dotnet build --warnaserror`, `dotnet test`,
  `dotnet list package --vulnerable --fail-build-on-warn`.
- **Aceite:** PR que reintroduz segredo ou `id` fixo é bloqueado.
- **Verificação:**
  ```bash
  dotnet build --warnaserror && dotnet test
  ```

### DEVOPS-04 · Branch `heads/origin` · [P2]

- **Arquivo:** git
- **Evidência:** `git rev-parse --abbrev-ref HEAD` → **`heads/origin`** (branch com nome de remote).
- **Impacto:** nome de branch não padrão pode confundir `git push` sem argumento e quebrar automações
  que assumem `main`/`master`.
- **Mudança:** renomear para `master` (ou `main`) e ajustar o remoto/upstream.
- **Aceite:** `git branch --show-current` → `master`; `git push` sem argumento funciona.
- **Verificação:**
  ```bash
  git branch -m master && git branch --show-current
  ```

---

## 7. Documentação

### DOC-01 · README não documenta variáveis nem rotação · [P2]

- **Arquivo:** `README.md` (103 linhas)
- **Evidência:** não lista `JWT_SecurityKey` como variável, nem explica que a chave **precisa**
  ser forte e **rotacionada**.
- **Impacto:** após o `SEC-01`, a API **não sobe** sem a variável — sem doc, ninguém descobre.
- **Mudança:** seção "Variáveis" (`JWT_SecurityKey` com `openssl rand -hex 32`, `JWT_Issuer`,
  `JWT_Audience`, `DatabaseSetting`) + aviso de **rotacionar** a chave se vazar.
- **Aceite:** README lista as 4 variáveis e o comando de geração.
- **Verificação:** `grep -n 'JWT_SecurityKey' README.md`.

### DOC-02 · Falta `SECURITY.md` · [P3]

- **Arquivo:** *(ausente)* `SECURITY.md`
- **Evidência:** tem `LICENSE`/README.
- **Impacto:** canal de reporte e as garantias (chave fora do git, `iss`/`aud` validados) não ficam
  escritas.
- **Mudança:** criar com canal + as invariantes: "chave só em env", "`iss`/`aud` validados",
  "claims vêm do usuário autenticado, nunca literais".
- **Aceite:** arquivo existe com as 3 invariantes.
- **Verificação:** `ls SECURITY.md`

---

## 8. Ordem de execução (waves)

### Wave 1 — Parar o vazamento e a impersonação (P0)
1. **`SEC-01`** — chave fora do `appsettings.json` versionado + **rotacionar**.
2. **`BUG-02`** — `GenerateToken(user)` (a assinatura) e **`SEC-03`** — claims reais.
3. **`SEC-02`** — validade de 30 dias → 1 h.

> Depois da Wave 1, não há chave pública nem token que finja ser o usuário 1.

### Wave 2 — Endurecer o token (P1)
4. **`SEC-05`** + **`SEC-04`** — emitir e validar `iss`/`aud`.
5. **`SEC-06`** — extração de header com validação.
6. **`IMP-01`** — controllers, **só depois** dos P0.
7. **`TEST-01`** — testes de token (o do `sub` é o que trava o `SEC-03`).
8. **`DEVOPS-01`** — net8.0 + CVEs.

### Wave 3 — Operação (P2)
9. **`BUG-01`** — remover `LifetimeValidator` duplicado; **`BUG-03`** — `ILogger`.
10. **`IMP-02`** — config uma vez + validação no boot.
11. **`DEVOPS-02`**, **`DEVOPS-03`**, **`DEVOPS-04`**.
12. **`SEC-07`**, **`DOC-01`**.

### Wave 4 — Registro (P3)
13. **`DOC-02`**.

**Dependências que não podem ser invertidas:**
`SEC-01` antes de tudo (chave publicada invalida qualquer outra defesa) · `BUG-02` **antes** de
`SEC-03` (a assinatura é a causa) · `SEC-03` antes de `IMP-01` (não criar rota com token errado) ·
`SEC-05` junto com `SEC-04` · `SEC-01` antes de `IMP-02` (a validação do boot usa a chave) ·
`TEST-01` depois dos P0 (testa o comportamento corrigido).

---

## 9. Fora de escopo / riscos

| Item | Decisão | Motivo |
|---|---|---|
| Reescrever o token como sessão/cookie | **Não** | O JWT está bem estruturado; os defeitos são identidade e validade. |
| Trocar Mongo por SQL | **Não** | Fora do escopo. |
| Implementar refresh token agora | **Não, ainda** | Reduzir para 1 h resolve a exposição (ver `SEC-02`); refresh é evolução. |
| Criar os controllers faltantes | **Não, não agora** | Primeiro os P0 — ver `IMP-01`. |
| Migrar para minimal API / .NET 8 style | **Não** | Refactor sem ganho de segurança. |

**Riscos desta execução:**

- **`SEC-01` exige rotação sua.** A IA tira do arquivo; **só você** gera a nova chave e decide o
  impacto de invalidar os tokens emitidos (todos caem).
- **`SEC-02` (30 d → 1 h) muda a UX** — sem refresh, o usuário é deslogado a cada hora. Se isso
  incomodar, implemente refresh **no mesmo passo** (ou aceite 8 h como meio-termo, documentado).
- **`SEC-03` muda a assinatura de `ITokenService`** — como não há controller, o impacto é baixo
  **hoje**; será trivial quando `IMP-01` for feito. Faça antes.
- **`DEVOPS-04` renomear a branch** pode Affectar automações que fazem push por nome.

---

## 10. Definição de pronto (DoD)

**Segurança**
- [ ] `SEC-01` — chave fora do arquivo versionado; **nova chave gerada**; antiga invalidada
- [ ] `SEC-02` — validade ≤ 1 h (verificado por `exp - iat`)
- [ ] `SEC-03` — claims vêm do usuário; **nenhum literal de id**
- [ ] `SEC-04`/`SEC-05` — `iss`/`aud` emitidos **e** validados
- [ ] `SEC-06` — sem header → `401`, sem exceção
- [ ] `SEC-07` — `AllowedHosts` explícito

**Funcional**
- [ ] `BUG-02` — `GenerateToken` recebe o usuário
- [ ] `BUG-01` — sem `LifetimeValidator` customizado
- [ ] `BUG-03` — erro de token em `ILogger`, não `Console.Write`
- [ ] `IMP-01` — controllers com `[Authorize]` (após os P0)
- [ ] `IMP-02` — config lida uma vez + chave ≥ 32 bytes validada no boot

**Testes e infra**
- [ ] `TEST-01` — ≥ 5 testes; o do `sub` falha se voltar `"1"`
- [ ] `DEVOPS-01` — net8.0, `--vulnerable` vazio
- [ ] `DEVOPS-02` — `obj/`/`bin/` fora do índice
- [ ] `DEVOPS-03` — CI verde
- [ ] `DEVOPS-04` — branch `master`
- [ ] `DOC-01` — README com variáveis e rotação
- [ ] `DOC-02` — `SECURITY.md`

**Validação final:**
```bash
dotnet build --warnaserror && dotnet test
dotnet list package --vulnerable --include-transitive
grep -rn 'JWT_SecurityKey' appsettings.json && echo FALHA || echo OK
```

---

*Fim do plano. Gerado por leitura direta do código em 2026-10-02. Nenhum item já estava corrigido.*
