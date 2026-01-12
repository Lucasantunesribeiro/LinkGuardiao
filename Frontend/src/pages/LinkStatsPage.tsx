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
          borderColor: '#0f6c5e',
          backgroundColor: 'rgba(15, 108, 94, 0.18)',
          tension: 0.35
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
          backgroundColor: ['#0f6c5e', '#f2a65a', '#e8765e', '#1f5d8b', '#7a6bb7']
        }
      ]
    };
  }, [stats]);

  if (loading) {
    return (
      <div className="flex justify-center items-center min-h-[60vh]">
        <div className="loading-spinner h-10 w-10" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex justify-center items-center min-h-[60vh]">
        <div className="panel px-4 py-3 text-sm text-[#c14c32]">
          {error}
        </div>
      </div>
    );
  }

  if (!stats) {
    return (
      <div className="flex justify-center items-center min-h-[60vh]">
        <div className="text-stone-600">Link nao encontrado</div>
      </div>
    );
  }

  const shortUrl = `${API_ROOT}/r/${stats.shortCode}`;

  return (
    <div className="space-y-8">
      <div className="space-y-2">
        <span className="badge">Insights</span>
        <h1 className="page-title">Estatisticas do link</h1>
        <p className="text-sm text-stone-600">Resumo de desempenho e acessos em tempo real.</p>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <div className="stat-card">
          <p className="text-xs uppercase text-stone-400">URL Original</p>
          <p className="text-sm text-stone-700 break-all mt-2">{stats.originalUrl}</p>
        </div>
        <div className="stat-card">
          <p className="text-xs uppercase text-stone-400">URL Encurtada</p>
          <a href={shortUrl} className="text-sm text-[color:rgb(var(--sea))] break-all mt-2 block" target="_blank" rel="noreferrer">
            {shortUrl}
          </a>
        </div>
        <div className="stat-card">
          <p className="text-xs uppercase text-stone-400">Total de Cliques</p>
          <p className="text-3xl font-display mt-2">{stats.totalClicks}</p>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="panel p-5 lg:col-span-2">
          <h2 className="text-lg font-semibold mb-4">Cliques por data</h2>
          <Line data={clicksByDateData} />
        </div>
        <div className="panel p-5">
          <h2 className="text-lg font-semibold mb-4">Distribuicao por navegador</h2>
          {stats.browserStats.length > 0 ? (
            <Doughnut data={browserStatsData} />
          ) : (
            <p className="text-sm text-stone-500">Sem dados para exibir.</p>
          )}
        </div>
      </div>

      <div className="panel p-5">
        <h2 className="text-lg font-semibold mb-4">Top IPs</h2>
        {stats.topIpAddresses.length > 0 ? (
          <ul className="space-y-2">
            {stats.topIpAddresses.map(item => (
              <li key={item.ipAddress} className="flex justify-between text-sm text-stone-700">
                <span>{item.ipAddress}</span>
                <span>{item.count} cliques</span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="text-sm text-stone-500">Sem dados para exibir.</p>
        )}
      </div>
    </div>
  );
};

export default LinkStatsPage;
