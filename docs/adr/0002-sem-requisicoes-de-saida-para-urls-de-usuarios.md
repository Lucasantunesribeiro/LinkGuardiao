# 0002 — O backend não faz requisições para URLs de usuários

- Status: aceito
- Data: 2026-11-03

## Contexto

SSRF só existe quando o servidor faz requisições para endereços controlados pelo usuário (preview,
verificação de link, crawler, webhook). Hoje nenhum componente faz isso: a API apenas armazena a URL e
responde `302`; o `AnalyticsConsumer` só lê SQS e grava no DynamoDB. A única chamada de saída é para
serviços AWS/Redis/PostgreSQL configurados por variável de ambiente.

Validar o destino com resolução DNS no cadastro traria latência e falhas transitórias na criação de links
sem fechar o problema (o DNS pode mudar depois — *DNS rebinding*), e não protege o servidor porque ele
não se conecta ao destino.

## Decisão

- Manter o backend sem requisições de saída para URLs de usuários.
- Validar o destino apenas sintaticamente (ADR 0001), inclusive IPs literais em todas as representações.
- Qualquer funcionalidade futura que busque URLs (preview, checagem de status, webhook) deve, no mínimo:
  1. usar um `HttpClient` dedicado com `SocketsHttpHandler.ConnectCallback` que resolve o DNS, aplica
     `IpAddressPolicy.IsPublic` a **cada** endereço e conecta no IP validado (impede rebinding entre a
     checagem e a conexão);
  2. desabilitar `AllowAutoRedirect` e seguir no máximo poucos redirects manualmente, revalidando cada
     `Location` com `UrlSafety.TryNormalize`;
  3. definir timeout total curto e limite de bytes lidos da resposta;
  4. não usar proxy do sistema e não repassar credenciais/cookies.

## Consequências

- Não há superfície de SSRF no servidor hoje; o risco residual de destinos internos é do navegador de quem
  clica, mitigado pela ADR 0001 apenas para IPs literais e nomes internos conhecidos.
- Domínios como `127.0.0.1.nip.io` continuam aceitos; registrado como limitação no README.
