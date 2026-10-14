# Plan — api_loja_venda_app

## 1. Visão geral
API .NET 10 de loja/vendas com JWT estrito (HS384, finalidade/emissor/audiência/expiração), segredo obrigatório por env, Mongo sem credenciais embutidas e Swagger restrito a Development. Revisão 2026-10-11 confirmou implementação no HEAD sincronizado.

## 2. Arquitetura e tecnologias
.NET 10, MongoDB, JWT. Branch de trabalho chamada `origin` (decisão de histórico mantida).

## 3. Funcionalidades existentes
- [x] Endpoints protegidos com JWT validado — implementado.
- [x] Config validada no startup; Swagger só em dev — implementado.
- [x] CI dotnet restore/build/audit — **modernizada nesta sessão** (Ubuntu 24.04, actions SHA-pinadas: checkout v7, setup-dotnet v6).

## 4. Objetivos futuros
- OBJ-01 (P2, concluído nesta sessão): CI previsível.
- OBJ-02 (P3): TEST-01 — projeto de testes (xUnit) para as rotas (vedado nesta esteira).
- OBJ-03 (P3, externo): rotação de segredos antigos e configuração de produção real (dono).

## 5–9. Melhorias/Roadmap/Riscos/Ideias
- Decisão registrada: `appsettings.json`/`.env.example` mantêm chaves vazias — nunca comitar segredos reais.
- Risco externo: credenciais históricas continuam no histórico Git (limpeza coordenada pendente do mantenedor).

*(Gerado em 2026-10-14 pelo agente orquestrador)*
