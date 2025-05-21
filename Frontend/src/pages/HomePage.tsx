import { Link } from 'react-router-dom';
import { FiLink, FiBarChart2, FiShield, FiClock } from 'react-icons/fi';
import { useAuth } from '../context/AuthContext';
import { MainLayout } from "../components/MainLayout";
import { DashboardHero } from '../components/DashboardHero';
import { Button } from '../components/Button';
import { Hero } from '../components/Hero';

const HomePage = () => {
  const { user } = useAuth();

  return (
    <MainLayout>
      {user ? (
        <DashboardHero>
          <div className="flex flex-col items-center justify-center gap-4 py-8">
            <div className="flex items-center gap-3">
              <span className="inline-flex items-center justify-center rounded-full bg-blue-500 text-white w-12 h-12 text-2xl shadow-lg">
                👋
              </span>
              <h1 className="text-3xl md:text-4xl font-extrabold text-gray-900">Bem-vindo de volta, {user.name}!</h1>
            </div>
            <p className="text-lg text-gray-700 max-w-xl text-center">Veja, gerencie e proteja seus links encurtados com facilidade.</p>
            <Button href="/dashboard" className="mt-4 px-8 py-3 text-lg rounded-lg shadow-md bg-blue-600 hover:bg-blue-700 transition-colors">
              Ir para Dashboard
            </Button>
          </div>
        </DashboardHero>
      ) : (
        <Hero>
          <h1>Encurte e proteja seus links</h1>
          <p>LinkGuardião é a ferramenta completa para encurtar, proteger e acompanhar o desempenho dos seus links</p>
          <Button>Começar agora</Button>
          <Button>Fazer login</Button>
        </Hero>
      )}

      {/* Features Section */}
      <section className="py-16 bg-white">
        <div className="container-custom mx-auto">
          <h2 className="text-3xl font-bold text-center mb-12">Recursos Poderosos</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
            <div className="card flex flex-col items-center text-center">
              <div className="bg-blue-100 p-4 rounded-full mb-4">
                <FiLink size={32} className="text-blue-600" />
              </div>
              <h3 className="text-xl font-semibold mb-2">Encurtamento de URLs</h3>
              <p className="text-gray-600">
                Transforme longas URLs em links curtos e fáceis de compartilhar com apenas um clique
              </p>
            </div>
            
            <div className="card flex flex-col items-center text-center">
              <div className="bg-blue-100 p-4 rounded-full mb-4">
                <FiBarChart2 size={32} className="text-blue-600" />
              </div>
              <h3 className="text-xl font-semibold mb-2">Estatísticas Detalhadas</h3>
              <p className="text-gray-600">
                Acompanhe cliques, localização geográfica, dispositivos e muito mais em tempo real
              </p>
            </div>
            
            <div className="card flex flex-col items-center text-center">
              <div className="bg-blue-100 p-4 rounded-full mb-4">
                <FiShield size={32} className="text-blue-600" />
              </div>
              <h3 className="text-xl font-semibold mb-2">Proteção com Senha</h3>
              <p className="text-gray-600">
                Adicione uma camada extra de segurança com proteção por senha para seus links importantes
              </p>
            </div>
            
            <div className="card flex flex-col items-center text-center">
              <div className="bg-blue-100 p-4 rounded-full mb-4">
                <FiClock size={32} className="text-blue-600" />
              </div>
              <h3 className="text-xl font-semibold mb-2">Expiração Automática</h3>
              <p className="text-gray-600">
                Configure links para expirarem automaticamente após um período específico
              </p>
            </div>
          </div>
        </div>
      </section>

      {!user && (
        <section className="bg-gray-100 py-16">
          <div className="container-custom mx-auto text-center">
            <h2 className="text-3xl font-bold mb-6">Pronto para começar?</h2>
            <p className="text-xl mb-8 max-w-2xl mx-auto">
              Crie sua conta gratuita agora e comece a gerenciar seus links de forma mais eficiente
            </p>
            <Link to="/register" className="btn-primary text-lg py-3 px-8">
              Registre-se Gratuitamente
            </Link>
          </div>
        </section>
      )}
    </MainLayout>
  );
};

export default HomePage;