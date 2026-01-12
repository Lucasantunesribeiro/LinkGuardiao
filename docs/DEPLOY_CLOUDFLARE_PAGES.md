# Deploy Cloudflare Pages (Frontend)

## Build settings
- Root directory: `Frontend`
- Build command: `npm ci && npm run build`
- Output directory: `dist`

## Environment variables
- `VITE_API_BASE_URL=https://api.seudominio.com`

## Steps
1. Acesse Cloudflare Pages e crie um novo projeto.
2. Conecte o repositorio e selecione o branch desejado.
3. Configure o root, build command e output conforme acima.
4. Adicione a variavel `VITE_API_BASE_URL`.
5. Execute o primeiro deploy.

## Custom domain
1. Em Pages > Custom domains, adicione `app.seudominio.com`.
2. Ajuste o DNS para apontar o CNAME conforme o painel.

## SPA routing
O arquivo `Frontend/public/_redirects` garante fallback para `index.html`.
