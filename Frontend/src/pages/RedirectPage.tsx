import { useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { API_ROOT } from '../lib/config';

const RedirectPage = () => {
  const { shortCode } = useParams();

  useEffect(() => {
    if (shortCode) {
      window.location.href = `${API_ROOT}/r/${shortCode}`;
    }
  }, [shortCode]);

  return (
    <div className="min-h-screen flex items-center justify-center">
      <div className="text-center space-y-4">
        <div className="loading-spinner h-12 w-12 mx-auto" />
        <p className="text-stone-600">Redirecionando...</p>
      </div>
    </div>
  );
};

export default RedirectPage; 
