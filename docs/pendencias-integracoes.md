# Pendências de Integrações e Decisões Externas

Este documento registra pontos que não devem ser implementados em produção sem confirmação formal do cliente, do banco, da prefeitura ou da Receita Federal.

## Boletos

- Confirmar banco/API provider, credenciais, ambiente de homologação e regras de registro/cancelamento.
- Implementar provider real somente depois da documentação oficial.
- O provider atual é apenas local/placeholder e retorna: "Integração bancária ainda não configurada."

## NFS-e

- Confirmar documentação oficial do município e se o ambiente é IPM/Atende.Net, NFS-e Nacional ou outro provider.
- Confirmar credenciais, URLs de homologação/produção, método de assinatura ICP-Brasil A1 e layout XML.
- O fluxo atual é manual/semi-manual: preparar dados no Monthoya, emitir no portal municipal e registrar número, código de verificação, PDF/XML e status no sistema.

## Certificado A1

- Não armazenar senha do certificado em texto puro.
- Não armazenar arquivo do certificado sem cofre/criptografia, controle de acesso e auditoria.
- Antes de automação: implementar armazenamento seguro, permissões por perfil, logs de auditoria, alertas de vencimento e separação homologação/produção.

## DIMOB

- Confirmar layout oficial vigente, campos obrigatórios, formato TXT e fluxo PGD/Receitanet antes de exportar arquivo final.
- A estrutura atual cobre conferência anual e base de coleta de dados, não entrega oficial.

## Documentos

- Modelos criados são iniciais ou pendentes de revisão.
- Contratos, notificações e recibos precisam de validação jurídica/operacional do cliente antes de uso definitivo.
