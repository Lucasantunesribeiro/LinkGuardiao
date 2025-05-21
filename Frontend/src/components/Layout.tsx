import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { FiHome, FiLink, FiUser, FiLogOut, FiPlus } from 'react-icons/fi';

interface LayoutProps {
  children: React.ReactNode;
}

const Layout: React.FC<LayoutProps> = ({ children }) => {
  const { user, signOut } = useAuth();
  const location = useLocation();

  const isAuthPage = location.pathname === '/login' || location.pathname === '/register';

  if (isAuthPage) {
    return (
      <div className="min-h-screen bg-gray-50">
        <div className="animate-fadeIn">
          {children}
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="navbar">
        <div className="navbar-content">
          <div className="flex items-center">
            <Link to="/" className="flex items-center space-x-2">
              {/* <img src="/logo.png" alt="LinkGuardião" className="h-8 w-8" /> */}
              <span className="text-xl font-bold text-gray-900">LinkGuardião</span>
            </Link>
          </div>

          {user ? (
            <div className="flex items-center space-x-6">
              <Link
                to="/"
                className="icon-button group relative"
                title="Início"
              >
                <FiHome className="h-5 w-5 text-gray-600" />
                <span className="tooltip">Início</span>
              </Link>

              <Link
                to="/dashboard"
                className="icon-button group relative"
                title="Meus Links"
              >
                <FiLink className="h-5 w-5 text-gray-600" />
                <span className="tooltip">Meus Links</span>
              </Link>
              
              <Link
                to="/create-link"
                className="icon-button group relative"
                title="Criar Link"
              >
                <FiPlus className="h-5 w-5 text-gray-600" />
                <span className="tooltip">Criar Link</span>
              </Link>

              <div className="flex items-center space-x-3 border-l pl-6 border-gray-200">
                <div className="flex items-center space-x-2">
                  <div className="h-8 w-8 rounded-full bg-blue-100 flex items-center justify-center">
                    <FiUser className="h-5 w-5 text-blue-600" />
                  </div>
                  <span className="text-sm font-medium text-gray-700">
                    {user.name}
                  </span>
                </div>

                <button
                  onClick={signOut}
                  className="icon-button group relative"
                  title="Sair"
                >
                  <FiLogOut className="h-5 w-5 text-gray-600" />
                  <span className="tooltip">Sair</span>
                </button>
              </div>
            </div>
          ) : (
            <div className="flex items-center space-x-4">
              <Link
                to="/login"
                className="btn-secondary"
              >
                Entrar
              </Link>
              <Link
                to="/register"
                className="btn-primary"
              >
                Registrar
              </Link>
            </div>
          )}
        </div>
      </nav>

      <main className="container-custom animate-fadeIn">
        {children}
      </main>

      <footer className="bg-white border-t border-gray-200 py-8 mt-auto">
        <div className="container-custom">
          <div className="flex flex-col items-center justify-center space-y-4">
            {/* <img src="/logo.png" alt="LinkGuardião" className="h-8 w-8" /> */}
            <p className="text-sm text-gray-600">
              © {new Date().getFullYear()} LinkGuardião. Todos os direitos reservados.
            </p>
          </div>
        </div>
      </footer>
    </div>
  );
};

export default Layout;