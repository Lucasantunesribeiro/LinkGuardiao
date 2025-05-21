# LinkGuardião 🔗🛡️

LinkGuardião é um sistema completo de encurtamento de URLs com proteção e estatísticas. O projeto oferece uma solução para criar, gerenciar e monitorar links curtos, com recursos avançados como proteção por senha, expiração automática e análise detalhada de acessos.

![LinkGuardião Banner](https://via.placeholder.com/1200x300/4f6eff/FFFFFF?text=LinkGuardi%C3%A3o)

## Recursos ✨

- **Encurtamento de URLs**: Transforme longas URLs em links curtos e fáceis de compartilhar
- **Estatísticas Detalhadas**: Acompanhe cliques, localização geográfica, dispositivos e muito mais
- **Proteção com Senha**: Adicione uma camada extra de segurança para seus links importantes
- **Expiração Automática**: Configure links para expirarem após um período específico
- **Dashboard Intuitivo**: Interface amigável para gerenciar todos os seus links
- **Sistema de Contas**: Registro e login para manter seus links organizados
- **Responsivo**: Funciona perfeitamente em dispositivos móveis e desktop

## Tecnologias Utilizadas 🚀

### Backend
- ASP.NET Core 8.0
- Entity Framework Core
- SQLite (para desenvolvimento local)
- JWT para autenticação
- Swagger para documentação da API

### Frontend
- React
- TypeScript
- Vite (para build e desenvolvimento)
- React Router
- Axios
- Tailwind CSS
- Chart.js para visualização de dados
- Formik e Yup para validação de formulários

## Implantação Gratuita ☁️

O LinkGuardião foi projetado para ser implantado gratuitamente em várias plataformas:

### Backend
- **Opção 1**: Azure App Service (nível gratuito)
- **Opção 2**: Render (plano gratuito)
- **Opção 3**: Railway (plano gratuito com limitações)

### Frontend
- **Opção 1**: Vercel (implementação gratuita)
- **Opção 2**: Netlify (implementação gratuita)
- **Opção 3**: GitHub Pages (gratuito)

### Banco de Dados
- **Desenvolvimento**: SQLite (arquivo local)
- **Produção**: Supabase (plano gratuito) ou Azure SQL (nível gratuito)

## Instalação e Execução Local 🖥️

### Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (v18+ recomendado)
- [npm](https://www.npmjs.com/) ou [yarn](https://yarnpkg.com/)

### Backend
```bash
# Clone o repositório
git clone https://github.com/lucasantunesribeiro/LinkGuardiao.git
cd LinkGuardiao/Backend

# Restaure as dependências
dotnet restore

# Execute as migrações do banco de dados
dotnet ef database update

# Inicie a API
dotnet run
```

### Frontend
```bash
# Navegue até a pasta do frontend
cd ../Frontend

# Instale as dependências
npm install
# ou
yarn

# Inicie o servidor de desenvolvimento
npm run dev
# ou
yarn dev
```

## Estrutura do Projeto 📁

```
LinkGuardiao/
│
├── Backend/                # API ASP.NET Core
│   ├── Controllers/        # Controladores da API
│   ├── Models/             # Modelos de dados
│   ├── Services/           # Serviços e lógica de negócios
│   ├── Data/               # Contexto do EF Core e migrações
│   └── DTOs/               # Objetos de transferência de dados
│
└── Frontend/               # Aplicação React
    ├── public/             # Arquivos estáticos
    ├── src/                # Código-fonte
    │   ├── components/     # Componentes reutilizáveis
    │   ├── pages/          # Páginas da aplicação
    │   ├── context/        # Contextos React (ex: Auth)
    │   └── services/       # Serviços (ex: API)
    └── ...
```

## Contribuindo 🤝

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues ou enviar pull requests.

1. Faça um fork do projeto
2. Crie sua branch de recurso (`git checkout -b feature/AmazingFeature`)
3. Commit suas alterações (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## Licença 📝

Este projeto está licenciado sob a licença MIT - veja o arquivo LICENSE para detalhes.

## Contato 📧

Seu Nome - [seu-email@exemplo.com](mailto:seu-email@exemplo.com)

Link do Projeto: [https://github.com/seu-usuario/LinkGuardiao](https://github.com/seu-usuario/LinkGuardiao)