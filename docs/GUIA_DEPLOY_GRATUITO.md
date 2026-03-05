# Guia de Deploy 100% Gratuito (Cloudflare Pages + Oracle VM Micro)

Este guia coloca o LinkGuardiao no ar sem custos, usando SQLite e cloudflared.

## Arquitetura
- Frontend: Cloudflare Pages (HTTPS)
- Backend: Oracle Cloud VM.Standard.E2.1.Micro (1GB RAM)
- Banco: SQLite local (arquivo)
- HTTPS da API: cloudflared (tunnel para http://127.0.0.1:8080)
- Nao requer dominio proprio

## 1) Frontend (Cloudflare Pages)
1. Conecte o repo no Cloudflare Pages.
2. Build command: `npm ci && npm run build`
3. Output directory: `Frontend/dist`
4. Crie a variavel `VITE_API_BASE_URL` depois de subir o backend.

## 2) Criar a VM na Oracle Cloud
1. Image: Ubuntu 22.04 ou Oracle Linux 8/9.
2. Shape: VM.Standard.E2.1.Micro.
3. Networking: subnet publica (porta 22 aberta para SSH).
4. Nao e necessario abrir portas 80/443.

## 3) Preparar a VM
Acesse via SSH:
```bash
ssh -i "caminho/para/chave.key" opc@IP_DA_VM
```

Instale .NET 8 runtime (exemplo Ubuntu):
```bash
sudo apt-get update
sudo apt-get install -y dotnet-runtime-8.0
```

Instale cloudflared:
```bash
curl -L https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64 -o cloudflared
sudo install -m 755 cloudflared /usr/local/bin/cloudflared
```

## 4) Configurar variaveis do backend
Edite `.env.prod` localmente:
- `ConnectionStrings__Default=Data Source=/var/lib/linkguardiao/linkguardiao.db`
- `Jwt__Secret` forte (>= 32 chars)
- `Cors__AllowedOrigins__0=https://9b5eff2a.linkguardiao.pages.dev`

## 5) Deploy sem Docker
No seu computador:
```bash
export SSH_HOST="IP_DA_VM"
export SSH_USER="opc"
export SSH_KEY_PATH="caminho/para/chave.key"
./scripts/deploy_oracle_sqlite.sh
```

O script:
- publica a API
- copia artefatos, `.env.prod` e services
- cria `/var/lib/linkguardiao`
- registra systemd e reinicia a API + cloudflared

## 6) Obter URL do tunnel e configurar o frontend
No servidor:
```bash
journalctl -u cloudflared -n 50 --no-pager
```
Copie o HTTPS exibido (ex: `https://xxxx.trycloudflare.com`) e configure:
- Cloudflare Pages > Settings > Environment variables
- `VITE_API_BASE_URL=https://xxxx.trycloudflare.com`

## 7) Rollback rapido
Os backups ficam em `$HOME/linkguardiao/backup-*.tgz`.
```bash
sudo systemctl stop linkguardiao-api
sudo rm -rf /opt/linkguardiao
sudo mkdir -p /opt/linkguardiao
sudo tar -xzf /home/opc/linkguardiao/backup-YYYYMMDDHHMMSS.tgz -C /opt/linkguardiao
sudo systemctl start linkguardiao-api
```

## Troubleshooting
- Mixed content: `VITE_API_BASE_URL` precisa ser HTTPS.
- 429: limite de taxa atingido (aguarde e tente novamente).
- SQLite sem permissão: verifique `chown -R linkguardiao:linkguardiao /var/lib/linkguardiao`.

## Servicos systemd
- API: `/etc/systemd/system/linkguardiao-api.service`
- Tunnel: `/etc/systemd/system/cloudflared.service`
- Env: `/etc/linkguardiao/env`
- DB: `/var/lib/linkguardiao/linkguardiao.db`

Comandos uteis:
```bash
sudo systemctl status linkguardiao-api
sudo systemctl status cloudflared
journalctl -u linkguardiao-api -n 100 --no-pager
```
