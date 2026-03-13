import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { API_ROOT, API_BASE_URL } from '../lib/config';
import { LinkAccessGrantResponse, PublicLinkLookup } from '../lib/api/types';
import { redirectToUrl } from '../lib/navigation';

const redirectToResolvedLink = (shortCode: string, accessGrant?: string) => {
  const grantQuery = accessGrant ? `?grant=${encodeURIComponent(accessGrant)}` : '';
  redirectToUrl(`${API_ROOT}/r/${shortCode}${grantQuery}`);
};

const RedirectPage = () => {
  const { shortCode } = useParams();
  const [loading, setLoading] = useState(true);
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [verifying, setVerifying] = useState(false);

  useEffect(() => {
    if (!shortCode) return;

    const abortController = new AbortController();

    fetch(`${API_BASE_URL}/links/code/${shortCode}`, { signal: abortController.signal })
      .then(res => {
        if (!res.ok) {
          redirectToResolvedLink(shortCode);
          return null;
        }
        return res.json() as Promise<PublicLinkLookup>;
      })
      .then(data => {
        if (!data) return;
        if (!data.isPasswordProtected) {
          redirectToResolvedLink(shortCode);
          return;
        }
        setLoading(false);
      })
      .catch(error => {
        if (error instanceof DOMException && error.name === 'AbortError') {
          return;
        }

        redirectToResolvedLink(shortCode);
      });

    return () => abortController.abort();
  }, [shortCode]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!shortCode || !password.trim()) return;

    setVerifying(true);
    setError('');

    try {
      const res = await fetch(`${API_BASE_URL}/links/access-grant/${shortCode}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ password }),
      });

      if (res.ok) {
        const data = (await res.json()) as LinkAccessGrantResponse;
        redirectToResolvedLink(shortCode, data.accessGrant);
      } else if (res.status === 401) {
        setError('Senha incorreta. Tente novamente.');
      } else {
        setError('Erro ao verificar senha. Tente novamente.');
      }
    } catch {
      setError('Erro de conexão. Tente novamente.');
    } finally {
      setVerifying(false);
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center space-y-4">
          <div className="loading-spinner h-12 w-12 mx-auto" />
          <p className="text-stone-600">Redirecionando...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-stone-50">
      <div className="w-full max-w-sm bg-white rounded-xl shadow-sm border border-stone-200 p-8 space-y-6">
        <div className="text-center">
          <h1 className="text-xl font-semibold text-stone-800">Link protegido</h1>
          <p className="mt-1 text-sm text-stone-500">Digite a senha para acessar este link.</p>
        </div>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label htmlFor="password" className="block text-sm font-medium text-stone-700 mb-1">
              Senha
            </label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              className="w-full rounded-lg border border-stone-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-stone-400"
              placeholder="Digite a senha"
              autoFocus
            />
            {error && <p className="mt-1 text-xs text-red-600">{error}</p>}
          </div>
          <button
            type="submit"
            disabled={verifying || !password.trim()}
            className="w-full rounded-lg bg-stone-800 px-4 py-2 text-sm font-medium text-white hover:bg-stone-700 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {verifying ? 'Verificando...' : 'Acessar'}
          </button>
        </form>
      </div>
    </div>
  );
};

export default RedirectPage;
