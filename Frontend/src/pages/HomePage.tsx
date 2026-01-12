import { FiLink, FiBarChart2, FiShield, FiClock, FiArrowUpRight } from 'react-icons/fi';
import { useAuth } from '../features/auth/AuthContext';
import { MainLayout } from "../components/MainLayout";
import { Button } from '../components/Button';
import { Hero } from '../components/Hero';

const HomePage = () => {
  const { user } = useAuth();

  return (
    <MainLayout>
      {user ? (
        <section className="app-container py-10">
          <div className="card grid gap-8 lg:grid-cols-[1.2fr_1fr] items-center">
            <div className="space-y-5">
              <span className="badge">Bem-vindo de volta</span>
              <h1 className="page-title">Tudo sob controle, {user.username}.</h1>
              <p className="text-base text-stone-600">
                Crie links protegidos, monitore campanhas e tenha visao completa de performance sem complicacao.
              </p>
              <div className="flex flex-wrap items-center gap-3">
                <Button href="/dashboard" variant="primary" size="lg">
                  Acessar dashboard
                </Button>
                <Button href="/create-link" variant="outline" size="lg">
                  Novo link rapido
                </Button>
              </div>
            </div>
            <div className="grid gap-4">
              <div className="stat-card">
                <p className="text-xs uppercase text-stone-400">Atalhos prontos</p>
                <p className="text-3xl font-display mt-2">+32%</p>
                <p className="text-sm text-stone-500 mt-1">Performance media da semana</p>
              </div>
              <div className="stat-card">
                <p className="text-xs uppercase text-stone-400">Links protegidos</p>
                <p className="text-3xl font-display mt-2">100%</p>
                <p className="text-sm text-stone-500 mt-1">Acessos com senha e expiracao</p>
              </div>
            </div>
          </div>
        </section>
      ) : (
        <Hero>
          <div className="grid gap-10 lg:grid-cols-[1.2fr_1fr] items-center">
            <div className="space-y-6">
              <span className="badge">Seguranca, performance e clareza</span>
              <h1 className="text-4xl md:text-6xl leading-tight">
                Links curtos, estrategia longa. Tudo no mesmo lugar.
              </h1>
              <p className="text-lg text-stone-600">
                LinkGuardiao transforma URLs em ativos inteligentes com senha, expiracao e estatisticas
                que mostram o que realmente importa.
              </p>
              <div className="flex flex-wrap items-center gap-3">
                <Button href="/register" variant="primary" size="lg">
                  Comecar gratis
                </Button>
                <Button href="/login" variant="ghost" size="lg">
                  Ver meu painel
                </Button>
              </div>
              <div className="flex flex-wrap gap-4 text-sm text-stone-500">
                <span className="badge">Sem cartao</span>
                <span className="badge">Controle total</span>
                <span className="badge">Dados em tempo real</span>
              </div>
            </div>
            <div className="panel p-6 space-y-4 animate-rise">
              <div className="flex items-center justify-between">
                <span className="text-xs uppercase text-stone-400">Resumo da campanha</span>
                <span className="text-xs text-stone-500">Hoje</span>
              </div>
              <div className="rounded-2xl border border-[color:rgb(var(--line))] bg-white px-4 py-3">
                <p className="text-xs text-stone-400">Link curto</p>
                <div className="flex items-center justify-between mt-1">
                  <p className="font-semibold">lg.io/sucesso</p>
                  <FiArrowUpRight className="text-[color:rgb(var(--sea))]" />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div className="stat-card">
                  <p className="text-xs uppercase text-stone-400">Cliques</p>
                  <p className="text-2xl font-display mt-1">1.248</p>
                  <p className="text-xs text-stone-500">+18% ontem</p>
                </div>
                <div className="stat-card">
                  <p className="text-xs uppercase text-stone-400">Seguranca</p>
                  <p className="text-2xl font-display mt-1">Ativa</p>
                  <p className="text-xs text-stone-500">Senha e expiracao</p>
                </div>
              </div>
            </div>
          </div>
        </Hero>
      )}

      {/* Features Section */}
      <section className="app-container py-14">
        <div className="flex flex-col gap-3 text-center mb-10">
          <span className="badge mx-auto">Recursos essenciais</span>
          <h2 className="section-title">O poder de decidir quem acessa e quando.</h2>
          <p className="text-stone-600 max-w-2xl mx-auto">
            Nao e apenas um encurtador. E uma central para campanhas, acessos privados e conteudo estrategico.
          </p>
        </div>
        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
          {[
            {
              title: 'Encurtamento inteligente',
              description: 'Reduza URLs com padrao de marca e compartilhamento rapido.',
              icon: FiLink
            },
            {
              title: 'Estatisticas cirurgicas',
              description: 'Acompanhe cliques e tendencias com visao clara de impacto.',
              icon: FiBarChart2
            },
            {
              title: 'Protecao com senha',
              description: 'Garanta acesso somente a quem precisa com um clique.',
              icon: FiShield
            },
            {
              title: 'Expiracao automatica',
              description: 'Defina janelas de acesso e retire links do ar no tempo certo.',
              icon: FiClock
            }
          ].map((item) => (
            <div key={item.title} className="card flex flex-col gap-4">
              <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-[color:rgba(19,122,108,0.15)] text-[color:rgb(var(--sea))]">
                <item.icon size={22} />
              </div>
              <div>
                <h3 className="text-lg font-semibold">{item.title}</h3>
                <p className="text-sm text-stone-600 mt-2">{item.description}</p>
              </div>
            </div>
          ))}
        </div>
      </section>

      {!user && (
        <section className="app-container pb-16">
          <div className="card text-center space-y-4">
            <span className="badge mx-auto">Pronto para avancar?</span>
            <h2 className="section-title">Comece a proteger seus links hoje.</h2>
            <p className="text-stone-600 max-w-2xl mx-auto">
              Crie sua conta gratuita e tenha controle total de acesso, expiracao e desempenho em minutos.
            </p>
            <div className="flex flex-wrap justify-center gap-3">
              <Button href="/register" variant="secondary" size="lg">
                Registrar gratuitamente
              </Button>
              <Button href="/login" variant="outline" size="lg">
                Ja tenho conta
              </Button>
            </div>
          </div>
        </section>
      )}
    </MainLayout>
  );
};

export default HomePage;
