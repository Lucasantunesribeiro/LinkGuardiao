import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../lib/api/client';
import { ShortenedLink } from '../lib/api/types';
import { API_ROOT } from '../lib/config';
import { toast } from 'react-toastify';
import { FiPlus, FiEdit, FiTrash2, FiExternalLink, FiBarChart2 } from 'react-icons/fi';
import { Button } from '../components/Button';
import Loading from '../components/Loading';

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
    <div className="space-y-8">
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div className="space-y-2">
          <span className="badge">Seu acervo</span>
          <h1 className="page-title">Meus links</h1>
          <p className="text-sm text-stone-600">
            Gerencie, edite e acompanhe cada link com seguranca.
          </p>
        </div>
        <Button href="/create-link" variant="secondary" size="lg">
          <FiPlus />
          Criar novo link
        </Button>
      </div>

      {loading ? (
        <div className="flex justify-center items-center h-64">
          <Loading />
        </div>
      ) : fetchError ? (
        <div className="panel px-4 py-4 text-sm text-[#c14c32]">
          Erro ao carregar links. Verifique o servidor e tente novamente.
        </div>
      ) : links.length === 0 ? (
        <div className="card text-center space-y-4">
          <p className="text-stone-600">Voce ainda nao tem links encurtados.</p>
          <Button href="/create-link" variant="secondary">
            <FiPlus />
            Criar meu primeiro link
          </Button>
        </div>
      ) : (
        <div className="grid gap-4">
          {links.map((link) => {
            const shortUrl = `${API_ROOT}/r/${link.shortCode}`;
            return (
              <div key={link.id} className="card flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
                <div className="space-y-2">
                  <div className="flex items-center gap-3">
                    <h3 className="text-lg font-semibold">{link.title || 'Sem titulo'}</h3>
                    <span className={`badge ${link.isActive ? '' : 'opacity-60'}`}>
                      {link.isActive ? 'Ativo' : 'Inativo'}
                    </span>
                  </div>
                  <p className="text-sm text-stone-500 break-all">{link.originalUrl}</p>
                  <a
                    href={shortUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="inline-flex items-center gap-2 text-sm text-[color:rgb(var(--sea))] hover:text-[color:rgb(var(--sea-strong))]"
                  >
                    {shortUrl}
                    <FiExternalLink size={14} />
                  </a>
                  <p className="text-xs text-stone-400">Criado em {formatDate(link.createdAt)}</p>
                </div>
                <div className="flex items-center gap-2">
                  <Link to={`/edit-link/${link.id}`} className="btn-outline">
                    <FiEdit />
                    Editar
                  </Link>
                  <Link to={`/stats/${link.id}`} className="btn-ghost">
                    <FiBarChart2 />
                    Estatisticas
                  </Link>
                  <button onClick={() => handleDelete(link.id)} className="btn-ghost text-[#c14c32]">
                    <FiTrash2 />
                    Excluir
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};

export default DashboardPage; 
