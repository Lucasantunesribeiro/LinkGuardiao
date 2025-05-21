import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../services/api';
import { toast } from 'react-toastify';
import { FiLink, FiType, FiClock, FiLock } from 'react-icons/fi';

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
      
      console.log('Enviando dados para criação de link:', {
        OriginalUrl: originalUrl,
        Title: title || null,
        Password: password || null,
        ExpiresAt: expiresAt ? new Date(expiresAt).toISOString() : null
      });
      
      const response = await api.post('/links', {
        OriginalUrl: originalUrl,
        Title: title || null,
        Password: password || null,
        ExpiresAt: expiresAt ? new Date(expiresAt).toISOString() : null
      });
      
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
    <div className="container mx-auto px-4 py-8">
      <div className="max-w-2xl mx-auto">
        <h1 className="text-3xl font-bold mb-8">Criar Novo Link</h1>
        
        <form onSubmit={handleSubmit} className="bg-white shadow-md rounded-lg p-6">
          <div className="mb-4">
            <label className="block text-gray-700 text-sm font-bold mb-2" htmlFor="originalUrl">
              URL Original*
            </label>
            <div className="relative">
              <div className="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none">
                <FiLink className="text-gray-400" />
              </div>
              <input
                type="text"
                id="originalUrl"
                placeholder="https://exemplo.com/pagina-com-url-grande"
                value={originalUrl}
                onChange={(e) => setOriginalUrl(e.target.value)}
                className={`w-full py-2 pl-10 pr-3 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${errors.originalUrl ? 'border-red-500' : 'border-gray-300'}`}
              />
            </div>
            {errors.originalUrl && <p className="text-red-500 text-xs mt-1">{errors.originalUrl}</p>}
          </div>
          
          <div className="mb-4">
            <label className="block text-gray-700 text-sm font-bold mb-2" htmlFor="title">
              Título (opcional)
            </label>
            <div className="relative">
              <div className="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none">
                <FiType className="text-gray-400" />
              </div>
              <input
                type="text"
                id="title"
                placeholder="Meu link personalizado"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                className="w-full py-2 pl-10 pr-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
          
          <div className="mb-4">
            <label className="block text-gray-700 text-sm font-bold mb-2" htmlFor="password">
              Senha (opcional)
            </label>
            <div className="relative">
              <div className="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none">
                <FiLock className="text-gray-400" />
              </div>
              <input
                type="password"
                id="password"
                placeholder="Senha para proteger o link"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className={`w-full py-2 pl-10 pr-3 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${errors.password ? 'border-red-500' : 'border-gray-300'}`}
              />
            </div>
            {errors.password && <p className="text-red-500 text-xs mt-1">{errors.password}</p>}
          </div>
          
          <div className="mb-6">
            <label className="block text-gray-700 text-sm font-bold mb-2" htmlFor="expiresAt">
              Data de Expiração (opcional)
            </label>
            <div className="relative">
              <div className="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none">
                <FiClock className="text-gray-400" />
              </div>
              <input
                type="datetime-local"
                id="expiresAt"
                value={expiresAt}
                onChange={(e) => setExpiresAt(e.target.value)}
                className="w-full py-2 pl-10 pr-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
          
          <div className="flex items-center justify-between">
            <button
              type="button"
              onClick={() => navigate('/dashboard')}
              className="text-gray-700 bg-gray-200 hover:bg-gray-300 font-bold py-2 px-4 rounded focus:outline-none focus:ring-2 focus:ring-gray-500"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="bg-blue-500 hover:bg-blue-600 text-white font-bold py-2 px-4 rounded focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isSubmitting ? 'Criando...' : 'Criar Link'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default CreateLinkPage; 