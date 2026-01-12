import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../lib/api/client';
import { ShortenedLink } from '../lib/api/types';
import { API_ROOT } from '../lib/config';
import { toast } from 'react-toastify';
import { FiPlus, FiEdit, FiTrash2, FiExternalLink, FiBarChart2 } from 'react-icons/fi';

const DashboardPage = () => {
  const [links, setLinks] = useState<ShortenedLink[]>([]);
  const [loading, setLoading] = useState(true);
  const [fetchError, setFetchError] = useState(false);

  useEffect(() => {
    const fetchLinks = async () => {
      try {
        const response = await api.get('/links');
        console.log('Links recebidos:', response.data);
        setLinks(response.data);
        setFetchError(false);
      } catch (error: any) {
        console.error(
          'Error fetching links:',
          error.response?.status,
          error.response?.data
        );
        const errorMessage = error.response?.data?.message || 'Erro ao buscar links';
        setFetchError(true);
        toast.error(errorMessage);
      } finally {
        setLoading(false);
      }
    };

    fetchLinks();
  }, []);

  const handleDelete = async (id: number) => {
    if (!window.confirm('Tem certeza que deseja excluir este link?')) {
      return;
    }

    try {
      await api.delete(`/links/${id}`);
      setLinks(links.filter(link => link.id !== id));
      toast.success('Link excluído com sucesso!');
    } catch (error: any) {
      console.error(error);
      const errorMessage = error.response?.data?.message || 'Erro ao excluir link';
      toast.error(errorMessage);
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Meus Links</h1>
        <Link 
          to="/create-link"
          className="bg-blue-500 hover:bg-blue-600 text-white font-bold py-2 px-4 rounded flex items-center"
        >
          <FiPlus className="mr-2" />
          Criar Novo Link
        </Link>
      </div>

      {loading ? (
        <div className="flex justify-center items-center h-64">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
        </div>
      ) : fetchError ? (
        <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
          <p>Erro ao carregar links. Verifique o servidor e tente novamente.</p>
        </div>
      ) : links.length === 0 ? (
        <div className="bg-white shadow-md rounded-lg p-6 text-center">
          <p className="text-gray-600 mb-4">Você ainda não tem links encurtados.</p>
          <Link 
            to="/create-link" 
            className="bg-blue-500 hover:bg-blue-600 text-white font-medium py-2 px-4 rounded inline-flex items-center"
          >
            <FiPlus className="mr-2" />
            Criar seu primeiro link
          </Link>
        </div>
      ) : (
        <div className="bg-white shadow-md rounded-lg overflow-hidden">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Título</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Link Original</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Link Encurtado</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Criado em</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Ações</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {links.map((link) => (
                <tr key={link.id}>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm font-medium text-gray-900">
                      {link.title || 'Sem título'}
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <div className="text-sm text-gray-900 max-w-xs truncate">
                      {link.originalUrl}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-blue-600">
                      <a 
                        href={`${API_ROOT}/${link.shortCode}`} 
                        target="_blank" 
                        rel="noopener noreferrer"
                        className="hover:underline flex items-center"
                      >
                        {link.shortCode}
                        <FiExternalLink className="ml-1" size={14} />
                      </a>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-500">
                      {formatDate(link.createdAt)}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                      link.isActive 
                        ? 'bg-green-100 text-green-800' 
                        : 'bg-red-100 text-red-800'
                    }`}>
                      {link.isActive ? 'Ativo' : 'Inativo'}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                    <div className="flex space-x-2">
                      <Link 
                        to={`/edit-link/${link.id}`}
                        className="text-indigo-600 hover:text-indigo-900"
                      >
                        <FiEdit />
                      </Link>
                      <Link 
                        to={`/stats/${link.id}`}
                        className="text-blue-600 hover:text-blue-900"
                      >
                        <FiBarChart2 />
                      </Link>
                      <button 
                        onClick={() => handleDelete(link.id)}
                        className="text-red-600 hover:text-red-900"
                      >
                        <FiTrash2 />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

export default DashboardPage; 
