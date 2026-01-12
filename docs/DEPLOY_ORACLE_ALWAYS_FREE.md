# Deploy Oracle Cloud Always Free (Backend)

## 1) Criar VM
- Crie uma VM Always Free (Ampere A1 ou E2) com Ubuntu.
- Libere portas 22, 80 e 443 no Security List/NSG da VCN.

## 2) Instalar Docker e Compose
```bash
sudo apt-get update
sudo apt-get install -y ca-certificates curl gnupg
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
sudo usermod -aG docker $USER
```

## 3) Subir containers
- Copie `docker-compose.prod.yml` e `.env.prod` para `~/linkguardiao`.
- Exemplo de `.env.prod`:
  - `CONNECTIONSTRINGS__DEFAULT=Host=db;Database=linkguardiao;Username=postgres;Password=...`
  - `JWT__SECRET=...` (min 32 chars)
  - `CORS__ALLOWEDORIGINS__0=https://app.seudominio.com`
  - `GHCR_IMAGE=ghcr.io/seu-user/linkguardiao-api:latest`

```bash
cd ~/linkguardiao
docker compose -f docker-compose.prod.yml pull api
docker compose -f docker-compose.prod.yml up -d
```

## 4) Reverse proxy com HTTPS (Caddy)
```bash
sudo apt-get install -y debian-keyring debian-archive-keyring apt-transport-https
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | sudo gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' | sudo tee /etc/apt/sources.list.d/caddy-stable.list
sudo apt-get update
sudo apt-get install -y caddy
```

- Copie `Caddyfile` para `/etc/caddy/Caddyfile` e ajuste o dominio.
- Reinicie:
```bash
sudo systemctl reload caddy
```

## 5) Automacao via SSH
- Use `scripts/deploy_oracle.sh` para build/push e deploy remoto.
- Necessario: `GHCR_USER`, `GHCR_TOKEN`, `SSH_HOST`, `SSH_USER`, `SSH_KEY_PATH`.
