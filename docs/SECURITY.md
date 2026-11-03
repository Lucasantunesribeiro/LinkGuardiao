# Security

## Medidas implementadas
- Rate limiting por IP e por endpoint (429 + Retry-After)
- Validacao de URL de destino: apenas http/https, sem userinfo, sem caracteres de controle, IDN em punycode,
  bloqueio de hosts internos e IPs nao publicos em qualquer representacao (ver docs/adr/0001 e 0002)
- Revalidacao do destino no redirect (link legado inseguro responde 410)
- Codigos curtos com formato restrito e lista de reservados (docs/adr/0003)
- CORS restrito ao dominio do Cloudflare Pages
- JWT com issuer/audience e expiracao curta
- Hash de senha com PBKDF2
- Headers de seguranca basicos
- TTL no DynamoDB para expiracao e logs
- Contador diario por usuario com TTL
- Logs estruturados sem tokens ou secrets

## Observabilidade
- RequestId propagado em `X-Request-Id`
- Logs com endpoint, status e userId quando presente

## Recomendacoes
- Rotacione `JWT__SECRET` periodicamente
- Use `LINKLIMITS__DAILYUSERCREATELIMIT` conforme risco
- Ajuste `AccessRetentionDays` conforme privacidade
