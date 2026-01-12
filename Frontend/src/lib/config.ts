export const API_ROOT = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000';
export const API_BASE_URL = new URL('/api', API_ROOT).toString();
