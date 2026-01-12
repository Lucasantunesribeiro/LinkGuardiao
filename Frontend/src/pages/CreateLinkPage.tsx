import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../lib/api/client';
import { LinkCreateRequest } from '../lib/api/types';
import { toast } from 'react-toastify';
import { FiLink, FiType, FiClock, FiLock } from 'react-icons/fi';
import Input from '../components/Input';
import { Button } from '../components/Button';

const CreateLinkPage = () => {
  const [originalUrl, setOriginalUrl] = useState('');
  const [title, setTitle] = useState('');
  const [password, setPassword] = useState('');
  const [expiresAt, setExpiresAt] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errors, setErrors] = useState<{originalUrl?: string, title?: string, password?: string}>({});
  
  const navigate = useNavigate();

  const validateForm = () => {
    const newErrors: {originalUrl?: string, title?: string, password?: string} = {};
    let isValid = true;

    if (!originalUrl.trim()) {
      newErrors.originalUrl = 'A URL original é obrigatória';
      isValid = false;
    } else if (!/^(https?:\/\/)?([\da-z.-]+)\.([a-z.]{2,6})([/\w .-]*)*\/?$/.test(originalUrl)) {
      newErrors.originalUrl = 'Por favor, insira uma URL válida';
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
    
    if (!validateForm()) {
      return;
    }

    try {
      setIsSubmitting(true);
      
      const payload: LinkCreateRequest = {
        originalUrl,
        title: title || null,
        password: password || null,
        expiresAt: expiresAt ? new Date(expiresAt).toISOString() : null
      };

      const response = await api.post('/links', payload);
      
      console.log('Link criado com sucesso:', response.data);
      toast.success('Link criado com sucesso!');
      navigate('/dashboard');
    } catch (error: any) {
      console.error('Erro ao criar link:', error);
      const errorMessage = error.response?.data?.message || 'Ocorreu um erro ao criar o link';
      toast.error(errorMessage);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="grid gap-10 lg:grid-cols-[1fr_1.1fr] items-start">
      <div className="space-y-4">
        <span className="badge">Novo link</span>
        <h1 className="page-title">Crie um link protegido.</h1>
        <p className="text-stone-600">
          Defina senha, expiracao e titulo para manter o controle total sobre cada acesso.
        </p>
        <div className="panel px-4 py-4 text-sm text-stone-500">
          Dica: links com senha geram acessos mais qualificados.
        </div>
      </div>

      <form onSubmit={handleSubmit} className="panel p-8 space-y-6">
        <Input
          id="originalUrl"
          name="originalUrl"
          type="text"
          label="URL Original"
          icon={FiLink}
          placeholder="https://exemplo.com/pagina-com-url-grande"
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
          placeholder="Meu link personalizado"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
        />

        <Input
          id="password"
          name="password"
          type="password"
          label="Senha (opcional)"
          icon={FiLock}
          placeholder="Proteja seu link com senha"
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

        <div className="flex flex-wrap items-center justify-between gap-3">
          <Button
            type="button"
            onClick={() => navigate('/dashboard')}
            variant="ghost"
          >
            Cancelar
          </Button>
          <Button type="submit" disabled={isSubmitting} variant="primary">
            {isSubmitting ? 'Criando...' : 'Criar link'}
          </Button>
        </div>
      </form>
    </div>
  );
};

export default CreateLinkPage; 
