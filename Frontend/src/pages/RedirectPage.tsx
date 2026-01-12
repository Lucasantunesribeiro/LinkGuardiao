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
    <div className="min-h-screen flex items-center justify-center bg-gray-100">
      <div className="text-center">
        <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500 mx-auto"></div>
        <p className="mt-4 text-gray-600">Redirecionando...</p>
      </div>
    </div>
  );
};

export default RedirectPage; 
