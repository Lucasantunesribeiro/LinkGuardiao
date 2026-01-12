import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { api } from '../lib/api/client';
import { ShortenedLink } from '../lib/api/types';
import { toast } from 'react-toastify';
import { FiLink, FiType, FiClock, FiLock } from 'react-icons/fi';
import Input from '../components/Input';
import { Button } from '../components/Button';

const EditLinkPage = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [originalUrl, setOriginalUrl] = useState('');
  const [title, setTitle] = useState('');
  const [password, setPassword] = useState('');
  const [expiresAt, setExpiresAt] = useState('');
  const [isActive, setIsActive] = useState(true);
  const [removePassword, setRemovePassword] = useState(false);
  const [hasPassword, setHasPassword] = useState(false);
  const [loading, setLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errors, setErrors] = useState<{ originalUrl?: string; password?: string }>({});

  useEffect(() => {
    const fetchLink = async () => {
      try {
        setLoading(true);
        const response = await api.get<ShortenedLink>(`/links/${id}`);
        const link = response.data;

        setOriginalUrl(link.originalUrl);
        setTitle(link.title ?? '');
        setIsActive(link.isActive);
        setHasPassword(!!link.isPasswordProtected);
        setExpiresAt(link.expiresAt ? new Date(link.expiresAt).toISOString().slice(0, 16) : '');
      } catch (error) {
        toast.error('Erro ao carregar link.');
      } finally {
        setLoading(false);
      }
    };

    if (id) {
      fetchLink();
    }
  }, [id]);

  const validateForm = () => {
    const newErrors: { originalUrl?: string; password?: string } = {};
    let isValid = true;

    if (!originalUrl.trim()) {
      newErrors.originalUrl = 'A URL original e obrigatoria';
      isValid = false;
    }

    if (password && password.length < 4) {
      newErrors.password = 'A senha deve ter pelo menos 4 caracteres';
      isValid = false;
    }

    setErrors(newErrors);
    return isValid;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm() || !id) {
      return;
    }

    try {
      setIsSubmitting(true);
      await api.put(`/links/${id}`, {
        originalUrl,
        title: title || null,
        password: password || null,
        removePassword,
        isActive,
        expiresAt: expiresAt ? new Date(expiresAt).toISOString() : null
      });

      toast.success('Link atualizado com sucesso!');
      navigate('/dashboard');
    } catch (error) {
      toast.error('Erro ao atualizar link.');
    } finally {
      setIsSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center min-h-[60vh]">
        <div className="loading-spinner h-10 w-10" />
      </div>
    );
  }

  return (
    <div className="grid gap-10 lg:grid-cols-[1fr_1.1fr] items-start">
      <div className="space-y-4">
        <span className="badge">Editar link</span>
        <h1 className="page-title">Ajuste detalhes e seguranca.</h1>
        <p className="text-stone-600">
          Atualize destino, titulo e controle de expiração com rapidez.
        </p>
        <div className="panel px-4 py-4 text-sm text-stone-500">
          Desative o link se precisar interromper acessos imediatamente.
        </div>
      </div>

      <form onSubmit={handleSubmit} className="panel p-8 space-y-6">
        <Input
          id="originalUrl"
          name="originalUrl"
          type="text"
          label="URL Original"
          icon={FiLink}
          value={originalUrl}
          onChange={(e) => setOriginalUrl(e.target.value)}
          error={errors.originalUrl}
          required
        />

        <Input
          id="title"
          name="title"
          type="text"
          label="Titulo (opcional)"
          icon={FiType}
          value={title}
          onChange={(e) => setTitle(e.target.value)}
        />

        <Input
          id="password"
          name="password"
          type="password"
          label="Nova senha (opcional)"
          icon={FiLock}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          error={errors.password}
        />

        <Input
          id="expiresAt"
          name="expiresAt"
          type="datetime-local"
          label="Data de expiracao (opcional)"
          icon={FiClock}
          value={expiresAt}
          onChange={(e) => setExpiresAt(e.target.value)}
        />

        <div className="flex flex-col gap-3 text-sm text-stone-600">
          <label className="inline-flex items-center gap-2">
            <input
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
              className="rounded border-[color:rgb(var(--line))] text-[color:rgb(var(--sea))] focus:ring-[color:rgb(var(--sea))]"
            />
            Link ativo
          </label>
          {hasPassword && (
            <label className="inline-flex items-center gap-2">
              <input
                type="checkbox"
                checked={removePassword}
                onChange={(e) => setRemovePassword(e.target.checked)}
                className="rounded border-[color:rgb(var(--line))] text-[color:rgb(var(--sea))] focus:ring-[color:rgb(var(--sea))]"
              />
              Remover senha atual
            </label>
          )}
        </div>

        <div className="flex flex-wrap items-center justify-between gap-3">
          <Button
            type="button"
            onClick={() => navigate('/dashboard')}
            variant="ghost"
          >
            Cancelar
          </Button>
          <Button type="submit" disabled={isSubmitting} variant="primary">
            {isSubmitting ? 'Salvando...' : 'Salvar'}
          </Button>
        </div>
      </form>
    </div>
  );
};

export default EditLinkPage;
