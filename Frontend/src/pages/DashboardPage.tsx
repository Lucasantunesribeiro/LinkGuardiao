import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../lib/api/client';
import { getApiErrorMessage } from '../lib/api/errors';
import { ShortenedLink } from '../lib/api/types';
import { API_ROOT } from '../lib/config';
import { toast } from 'react-toastify';
import { FiPlus, FiEdit, FiTrash2, FiExternalLink, FiBarChart2, FiLink } from 'react-icons/fi';
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
        setLinks(response.data);
        setFetchError(false);
      } catch (error) {
        setFetchError(true);
        toast.error(getApiErrorMessage(error, 'Erro ao buscar links'));
      } finally {
        setLoading(false);
      }
    };

    fetchLinks();
  }, []);

  const handleDelete = async (id: string) => {
    if (!window.confirm('Tem certeza que deseja excluir este link?')) {
      return;
    }

    try {
      await api.delete(`/links/${id}`);
      setLinks(currentLinks => currentLinks.filter(link => link.id !== id));
      toast.success('Link excluído com sucesso!');
    } catch (error) {
      toast.error(getApiErrorMessage(error, 'Erro ao excluir link'));
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
            Gerencie, edite e acompanhe cada link com segurança.
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
        <div role="alert" className="panel flex flex-col gap-3 px-5 py-4 text-sm text-[#a63d26] sm:flex-row sm:items-center sm:justify-between">
          <span>Erro ao carregar links. Verifique o servidor e tente novamente.</span>
          <button type="button" className="btn-outline btn-sm" onClick={() => window.location.reload()}>
            Tentar novamente
          </button>
        </div>
      ) : links.length === 0 ? (
        <div className="card flex flex-col items-center gap-4 py-12 text-center">
          <span className="flex h-14 w-14 items-center justify-center rounded-2xl bg-[color:rgb(var(--sea))]/10 text-2xl text-[color:rgb(var(--sea))]" aria-hidden="true">
            <FiLink />
          </span>
          <div className="space-y-1">
            <h2 className="text-xl font-display">Nenhum link ainda</h2>
            <p className="text-sm text-stone-600">Crie o primeiro link curto e acompanhe os cliques por aqui.</p>
          </div>
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
                    <h3 className="text-lg font-semibold">{link.title || 'Sem título'}</h3>
                    <span className={`badge ${link.isActive ? 'border-[color:rgb(var(--sea))]/30 text-[color:rgb(var(--sea-strong))]' : 'text-stone-600'}`}>
                      {link.isActive ? 'Ativo' : 'Inativo'}
                    </span>
                  </div>
                  <p className="text-sm text-stone-600 break-all">{link.originalUrl}</p>
                  <a
                    href={shortUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="inline-flex items-center gap-2 break-all text-sm font-medium text-[color:rgb(var(--sea-strong))] hover:underline"
                  >
                    {shortUrl}
                    <FiExternalLink size={14} aria-hidden="true" className="shrink-0" />
                  </a>
                  <p className="text-xs text-stone-600">Criado em {formatDate(link.createdAt)}</p>
                </div>
                <div className="flex flex-wrap items-center gap-2">
                  <Link to={`/edit-link/${link.id}`} className="btn-outline">
                    <FiEdit />
                    Editar
                  </Link>
                  <Link to={`/stats/${link.id}`} className="btn-ghost">
                    <FiBarChart2 />
                    Estatísticas
                  </Link>
                  <button type="button" onClick={() => handleDelete(link.id)} className="btn-ghost text-[#a63d26]" aria-label={`Excluir ${link.title || link.shortCode}`}>
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
