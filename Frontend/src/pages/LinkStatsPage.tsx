import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { api } from '../services/api';

interface LinkStats {
  id: string;
  originalUrl: string;
  shortUrl: string;
  clicks: number;
  createdAt: string;
  lastClickedAt?: string;
}

const LinkStatsPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [stats, setStats] = useState<LinkStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchStats = async () => {
      try {
        setLoading(true);
        const response = await api.get(`/links/${id}/stats`);
        setStats(response.data);
      } catch (err) {
        setError('Erro ao carregar estatísticas do link');
        console.error('Erro:', err);
      } finally {
        setLoading(false);
      }
    };

    if (id) {
      fetchStats();
    }
  }, [id]);

  if (loading) {
    return (
      <div className="flex justify-center items-center min-h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex justify-center items-center min-h-screen">
        <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded">
          {error}
        </div>
      </div>
    );
  }

  if (!stats) {
    return (
      <div className="flex justify-center items-center min-h-screen">
        <div className="text-gray-600">Link não encontrado</div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold mb-6">Estatísticas do Link</h1>
      
      <div className="bg-white shadow-lg rounded-lg p-6">
        <div className="grid gap-4">
          <div>
            <h2 className="text-sm text-gray-600">URL Original</h2>
            <p className="text-lg break-all">{stats.originalUrl}</p>
          </div>
          
          <div>
            <h2 className="text-sm text-gray-600">URL Encurtada</h2>
            <p className="text-lg break-all">{stats.shortUrl}</p>
          </div>
          
          <div>
            <h2 className="text-sm text-gray-600">Total de Cliques</h2>
            <p className="text-2xl font-bold text-blue-600">{stats.clicks}</p>
          </div>
          
          <div>
            <h2 className="text-sm text-gray-600">Criado em</h2>
            <p className="text-lg">
              {new Date(stats.createdAt).toLocaleDateString('pt-BR')}
            </p>
          </div>
          
          {stats.lastClickedAt && (
            <div>
              <h2 className="text-sm text-gray-600">Último Clique</h2>
              <p className="text-lg">
                {new Date(stats.lastClickedAt).toLocaleDateString('pt-BR')}
              </p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default LinkStatsPage; 