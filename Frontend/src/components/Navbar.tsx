import { Link } from 'react-router-dom';
import { useState } from 'react';
import { useAuth } from '../features/auth/AuthContext';
import { Button } from './Button';
import { FiHome, FiLink, FiUser, FiLogOut, FiPlus, FiMenu, FiX } from 'react-icons/fi';

const Navbar = () => {
  const { isAuthenticated, signOut, user } = useAuth();
  const [isOpen, setIsOpen] = useState(false);

  return (
    <nav className="navbar">
      <div className="navbar-content">
        <Link to="/" className="flex items-center gap-3">
          <span className="flex h-10 w-10 items-center justify-center rounded-2xl bg-[color:rgb(var(--sea))] text-white shadow-soft-xl">
            <FiLink />
          </span>
          <div className="leading-tight">
            <p className="text-lg font-display">LinkGuardiao</p>
            <p className="text-xs text-stone-500">Encurte com controle</p>
          </div>
        </Link>

        <div className="hidden md:flex items-center gap-6">
          <Link to="/" className="flex items-center gap-2 text-sm text-stone-700 hover:text-ink transition">
            <FiHome />
            Inicio
          </Link>
          {isAuthenticated && (
            <Link to="/dashboard" className="flex items-center gap-2 text-sm text-stone-700 hover:text-ink transition">
              <FiLink />
              Meus links
            </Link>
          )}
        </div>

        <div className="hidden md:flex items-center gap-3">
          {isAuthenticated ? (
            <>
              <Link
                to="/create-link"
                className="btn-outline"
              >
                <FiPlus />
                Novo link
              </Link>
              <div className="flex items-center gap-3 rounded-full bg-white/70 px-4 py-2 border border-[color:rgb(var(--line))]">
                <span className="flex h-8 w-8 items-center justify-center rounded-full bg-[color:rgb(var(--sea))] text-white text-xs font-bold">
                  {user?.username?.slice(0, 1).toUpperCase()}
                </span>
                <span className="text-sm text-stone-700">Oi, {user?.username}</span>
                <button onClick={signOut} className="text-stone-500 hover:text-ink transition" aria-label="Sair">
                  <FiLogOut />
                </button>
              </div>
            </>
          ) : (
            <>
              <Button href="/login" variant="ghost">
                <FiUser />
                Entrar
              </Button>
              <Button href="/register" variant="secondary">
                Criar conta
              </Button>
            </>
          )}
        </div>

        <button
          className="md:hidden btn-ghost"
          onClick={() => setIsOpen(!isOpen)}
          aria-label="Abrir menu"
        >
          {isOpen ? <FiX /> : <FiMenu />}
        </button>
      </div>

      {isOpen && (
        <div className="md:hidden border-t border-white/70 bg-white/90 backdrop-blur">
          <div className="app-container py-4 flex flex-col gap-3">
            <Link to="/" className="flex items-center gap-2 text-sm text-stone-700" onClick={() => setIsOpen(false)}>
              <FiHome />
              Inicio
            </Link>
            {isAuthenticated && (
              <>
                <Link to="/dashboard" className="flex items-center gap-2 text-sm text-stone-700" onClick={() => setIsOpen(false)}>
                  <FiLink />
                  Meus links
                </Link>
                <Link to="/create-link" className="flex items-center gap-2 text-sm text-stone-700" onClick={() => setIsOpen(false)}>
                  <FiPlus />
                  Novo link
                </Link>
              </>
            )}
            <div className="flex flex-col gap-2 pt-2">
              {isAuthenticated ? (
                <button onClick={signOut} className="btn-outline justify-start">
                  <FiLogOut />
                  Sair
                </button>
              ) : (
                <>
                  <Button href="/login" variant="ghost" className="justify-start">
                    <FiUser />
                    Entrar
                  </Button>
                  <Button href="/register" variant="secondary" className="justify-start">
                    Criar conta
                  </Button>
                </>
              )}
            </div>
          </div>
        </div>
      )}
    </nav>
  );
};

export default Navbar;
