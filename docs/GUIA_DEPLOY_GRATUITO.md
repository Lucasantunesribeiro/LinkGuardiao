# Guia de Deploy 100% Gratuito (Cloudflare + Oracle Cloud)

Este guia explica como colocar o **LinkGuardiao** no ar sem gastar nada.

## Arquitetura
*   **Frontend**: Cloudflare Pages (Hospedagem estática global, HTTPS automático).
*   **Backend + Banco**: Oracle Cloud "Always Free" (VM ARM, Docker, Caddy com HTTPS via nip.io).

---

## 1. Frontend (Cloudflare Pages)

1.  Acesse [Cloudflare Dashboard](https://dash.cloudflare.com/) > **Pages**.
2.  Clique em **Connect to Git** e selecione o repositório `LinkGuardiao`.
3.  **Configurações de Build**:
    *   **Project Name**: `linkguardiao` (Isso criará a URL `https://linkguardiao.pages.dev`).
    *   **Framework Preset**: Vite (se houver) ou "None".
    *   **Build command**: `npm ci && npm run build`
    *   **Output directory**: `Frontend/dist`
    *   **Environment Variables** (Adicione depois de criar ou na tela de setup):
        *   `VITE_API_BASE_URL`: Deixe em branco por enquanto (vamos preencher após subir o backend).
4.  Clique em **Save and Deploy**.

> O site estará no ar, mas a API ainda não vai funcionar.

---

## 2. Backend (Oracle Cloud Infrastructure - OCI)

### 2.1 Criar a Máquina Virtual (VM)
1.  Crie uma conta na Oracle Cloud e vá em **Instances** > **Create Instance**.
2.  **Image**: Ubuntu 22.04 ou Oracle Linux 8/9.
3.  **Shape**: Escolha **Ampere (ARM)** VM.Standard.A1.Flex (é a mais potente do plano gratuito).
    *   *Nota*: Se não houver disponibilidade ARM, use a VM.Standard.E2.1.Micro (AMD), mas será mais lenta.
4.  **Networking**: Crie uma VCN com "Public Subnet".
5.  **SSH Keys**: Baixe a chave privada (`.key` ou `.pem`) e guarde com segurança. Você vai precisar dela!
6.  Clique em **Create**.

### 2.2 Liberar Portas (Firewall)
1.  Na página da instância, clique no link da **Subnet**.
2.  Clique em **Security Lists** (ex: `Default Security List...`).
3.  Adicione **Ingress Rules** para liberar HTTP e HTTPS:
    *   **Source**: `0.0.0.0/0`
    *   **Protocol**: TCP
    *   **Destination Port Range**: `80,443`
4.  (Opcional) Verifique se a porta `22` (SSH) já está liberada.

### 2.3 Preparar a VM
Acesse a VM via SSH (use Git Bash ou WSL no Windows):
```bash
ssh -i "caminho/para/sua-chave.key" ubuntu@IP_DA_SUA_VM
```
*(Se usar Oracle Linux, o usuário é `opc` em vez de `ubuntu`)*

Execute os comandos para instalar Docker:
```bash
# Atualizar sistema
sudo apt-get update && sudo apt-get upgrade -y

# Instalar Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Dar permissão ao seu usuário
sudo usermod -aG docker $USER
exit
```
**Importante**: Após o `exit`, logue novamente via SSH para validar as permissões.

---

## 3. Configuração Local e Deploy

Agora você vai enviar o código do seu computador para a VM.

### 3.1 Configurar Variáveis
1.  No seu computador, abra `scripts/deploy_oracle.sh`.
2.  Edite (ou exporte no terminal) as variáveis de conexão com o GitHub Container Registry (GHCR) e da VM.
    *   *Dica: Para não editar o script versionado, você pode apenas criar um arquivo `.env.deploy` e fazer `source .env.deploy` antes de rodar.*
3.  Edite o arquivo `.env.prod`:
    *   `DOMAIN_NAME`: Coloque o IP da sua VM seguido de `.nip.io`.
        *   Exemplo: Se o IP é `150.10.20.30`, coloque `150.10.20.30.nip.io`.
    *   `CORS__ALLOWEDORIGINS__0`: Coloque a URL do seu frontend (ex: `https://linkguardiao.pages.dev`).
    *   Defina senhas seguras para o banco e JWT.

### 3.2 Executar Deploy
No terminal (WSL ou Git Bash), na raiz do projeto:

```bash
# Exporte as credenciais (exemplo)
export GHCR_USER="seu-usuario-github"
export GHCR_TOKEN="seu-token-github-classic" # Token com permissão 'write:packages'
export SSH_HOST="IP_DA_SUA_VM"
export SSH_USER="ubuntu"
export SSH_KEY_PATH="caminho/para/chave.key"
export GHCR_IMAGE="ghcr.io/seu-usuario-github/linkguardiao-api"

# Execute o script
./scripts/deploy_oracle.sh
```

O script vai:
1.  Construir a imagem Docker do backend.
2.  Subir para o GitHub Container Registry.
3.  Copiar `docker-compose.prod.yml`, `.env.prod` e `Caddyfile` para o servidor.
4.  Rodar `docker compose up -d` remotamente.

---

## 4. Finalização

1.  Acesse `https://SEU_IP.nip.io/swagger` para ver se a API está no ar (pode levar 1-2 min para o Caddy gerar o certificado).
2.  Volte no **Cloudflare Pages**:
    *   Vá em **Settings > Environment variables**.
    *   Adicione/Edite `VITE_API_BASE_URL` com o valor `https://SEU_IP.nip.io`.
    *   Vá em **Deployments** e clique em **Retry deployment** (ou faça um novo commit).

Pronto! Sua aplicação está 100% no ar e gratuita.
