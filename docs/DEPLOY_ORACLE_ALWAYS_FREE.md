# Deploy Oracle Cloud Always Free (Backend)

Este projeto agora usa SQLite local e cloudflared (sem Docker).

Use o guia principal:
- `docs/GUIA_DEPLOY_GRATUITO.md`

Resumo rapido:
1) Instale .NET 8 runtime e cloudflared na VM.
2) Configure `.env.prod` com SQLite e CORS.
3) Rode `scripts/deploy_oracle_sqlite.sh`.
4) Verifique `systemctl status linkguardiao-api` e `cloudflared`.
