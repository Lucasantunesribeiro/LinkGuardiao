import { useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Line, Doughnut } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  ArcElement,
  Tooltip,
  Legend
} from 'chart.js';
import { api } from '../lib/api/client';
import { API_ROOT } from '../lib/config';
import { LinkStatsDto } from '../lib/api/types';

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, ArcElement, Tooltip, Legend);

const LinkStatsPage = () => {
  const { id } = useParams<{ id: string }>();
  const [stats, setStats] = useState<LinkStatsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchStats = async () => {
      try {
        setLoading(true);
        const response = await api.get<LinkStatsDto>(`/links/${id}/stats`);
        setStats(response.data);
      } catch (err) {
        setError('Erro ao carregar estatisticas do link');
      } finally {
        setLoading(false);
      }
    };

    if (id) {
      fetchStats();
    }
  }, [id]);

  const clicksByDateData = useMemo(() => {
    if (!stats) {
      return { labels: [], datasets: [] };
    }

    const labels = stats.clicksByDate.map(item =>
      new Date(item.date).toLocaleDateString('pt-BR')
    );

    return {
      labels,
      datasets: [
        {
          label: 'Cliques',
          data: stats.clicksByDate.map(item => item.count),
          borderColor: '#2563eb',
          backgroundColor: 'rgba(37, 99, 235, 0.2)',
          tension: 0.3
        }
      ]
    };
  }, [stats]);

  const browserStatsData = useMemo(() => {
    if (!stats) {
      return { labels: [], datasets: [] };
    }

    return {
      labels: stats.browserStats.map(item => item.browser),
      datasets: [
        {
          data: stats.browserStats.map(item => item.count),
          backgroundColor: ['#0ea5e9', '#14b8a6', '#f59e0b', '#ef4444', '#6366f1']
        }
      ]
    };
  }, [stats]);

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
        <div className="text-gray-600">Link nao encontrado</div>
      </div>
    );
  }

  const shortUrl = `${API_ROOT}/${stats.shortCode}`;

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-6">
        <h1 className="text-3xl font-bold">Estatisticas do Link</h1>
        <p className="text-sm text-gray-500 mt-2">Resumo de desempenho e acessos.</p>
      </div>

      <div className="grid gap-4 md:grid-cols-3 mb-6">
        <div className="bg-white shadow-sm rounded-lg p-4">
          <p className="text-xs uppercase text-gray-500">URL Original</p>
          <p className="text-sm text-gray-900 break-all mt-2">{stats.originalUrl}</p>
        </div>
        <div className="bg-white shadow-sm rounded-lg p-4">
          <p className="text-xs uppercase text-gray-500">URL Encurtada</p>
          <a href={shortUrl} className="text-sm text-blue-600 break-all mt-2 block" target="_blank" rel="noreferrer">
            {shortUrl}
          </a>
        </div>
        <div className="bg-white shadow-sm rounded-lg p-4">
          <p className="text-xs uppercase text-gray-500">Total de Cliques</p>
          <p className="text-3xl font-semibold text-gray-900 mt-2">{stats.totalClicks}</p>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="bg-white shadow-sm rounded-lg p-4 lg:col-span-2">
          <h2 className="text-lg font-semibold mb-4">Cliques por Data</h2>
          <Line data={clicksByDateData} />
        </div>
        <div className="bg-white shadow-sm rounded-lg p-4">
          <h2 className="text-lg font-semibold mb-4">Distribuicao por Navegador</h2>
          {stats.browserStats.length > 0 ? (
            <Doughnut data={browserStatsData} />
          ) : (
            <p className="text-sm text-gray-500">Sem dados para exibir.</p>
          )}
        </div>
      </div>

      <div className="bg-white shadow-sm rounded-lg p-4 mt-6">
        <h2 className="text-lg font-semibold mb-4">Top IPs</h2>
        {stats.topIpAddresses.length > 0 ? (
          <ul className="space-y-2">
            {stats.topIpAddresses.map(item => (
              <li key={item.ipAddress} className="flex justify-between text-sm text-gray-700">
                <span>{item.ipAddress}</span>
                <span>{item.count} cliques</span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="text-sm text-gray-500">Sem dados para exibir.</p>
        )}
      </div>
    </div>
  );
};

export default LinkStatsPage;
