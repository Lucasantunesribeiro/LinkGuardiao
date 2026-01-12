import { FiGithub } from 'react-icons/fi';

const Footer = () => {
  return (
    <footer className="border-t border-white/60 bg-white/50 backdrop-blur">
      <div className="app-container py-10 flex flex-col gap-6 md:flex-row md:items-center md:justify-between">
        <div>
          <p className="text-sm text-stone-600">
            © {new Date().getFullYear()} LinkGuardiao. Todos os direitos reservados.
          </p>
          <p className="text-xs text-stone-400 mt-1">
            Controle inteligente para links sensiveis e campanhas.
          </p>
        </div>
        <div className="flex items-center gap-3 text-sm text-stone-500">
          <a
            href="https://github.com/yourusername/linkguardiao"
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-2 rounded-full border border-[color:rgb(var(--line))] px-4 py-2 hover:text-ink hover:border-[color:rgb(var(--sea))] transition"
          >
            <FiGithub />
            GitHub
          </a>
        </div>
      </div>
    </footer>
  );
};

export default Footer;
