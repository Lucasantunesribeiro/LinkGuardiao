import { Link } from 'react-router-dom';

const NotFoundPage = () => {
  return (
    <div className="min-h-[70vh] flex items-center justify-center px-6">
      <div className="text-center max-w-md">
        <h1 className="text-4xl font-bold text-gray-900">Pagina nao encontrada</h1>
        <p className="text-gray-600 mt-3">
          O endereco acessado nao existe ou foi movido.
        </p>
        <Link
          to="/"
          className="inline-flex items-center justify-center mt-6 bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"
        >
          Voltar para a home
        </Link>
      </div>
    </div>
  );
};

export default NotFoundPage;
