const envApiRoot = import.meta.env.VITE_API_BASE_URL;

if (import.meta.env.PROD && !envApiRoot) {
  throw new Error('VITE_API_BASE_URL is required in production builds.');
}

export const API_ROOT = envApiRoot ?? 'http://localhost:5000';
export const API_BASE_URL = new URL('/api', API_ROOT).toString();
