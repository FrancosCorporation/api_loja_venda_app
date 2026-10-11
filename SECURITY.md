# Segurança

## Reportar uma vulnerabilidade

Use o recurso de reporte privado de vulnerabilidades do GitHub para este repositório. Não publique detalhes exploráveis em uma issue pública.

## Regras para segredos e tokens

- A chave de assinatura JWT deve existir somente em um cofre de segredos ou variável de ambiente `JWT_SECURITY_KEY`.
- Credenciais MongoDB devem ser fornecidas por `MONGODB_CONNECTION_STRING` no ambiente de implantação.
- Tokens são aceitos somente com assinatura HMAC-SHA384 válida, emissor e audiência configurados, validade ativa e finalidade esperada.
- O ID do usuário deve vir da conta autenticada em uma camada confiável; nunca use uma identidade fixa ou valor fornecido diretamente pelo cliente.
- Tokens de acesso duram no máximo 60 minutos e tokens de redefinição no máximo 120 minutos.

Se um segredo foi versionado, remova-o da configuração atual e rotacione-o imediatamente. Apagar o arquivo em um commit novo não remove o segredo do histórico Git; a reescrita coordenada do histórico requer atualização dos clones e do remoto.
