import { Link } from 'react-router-dom';

const NotFoundPage = () => {
  return (
    <div className="min-h-[60vh] flex items-center justify-center px-6">
      <div className="text-center max-w-md space-y-4">
        <span className="badge mx-auto">404</span>
        <h1 className="text-4xl font-display">Pagina nao encontrada</h1>
        <p className="text-stone-600">
          O endereco acessado nao existe ou foi movido.
        </p>
        <Link
          to="/"
          className="btn-primary inline-flex"
        >
          Voltar para a home
        </Link>
      </div>
    </div>
  );
};

export default NotFoundPage;
