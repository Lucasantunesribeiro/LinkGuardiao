import { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { api } from '../services/api';
import axios from 'axios';

interface User {
  id: number;
  name: string;
  email: string;
}

interface AuthContextType {
  user: User | null;
  loading: boolean;
  isAuthenticated: boolean;
  signIn: (email: string, password: string) => Promise<void>;
  signUp: (name: string, email: string, password: string) => Promise<void>;
  signOut: () => void;
  register: (name: string, email: string, password: string) => Promise<void>;
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
  children: ReactNode;
}

// Cria instância com proxy do Vite; envia token JWT em todas as requisições
const axiosInstance = axios.create({
  baseURL: '/api',
});
// Adiciona header Authorization automaticamente
axiosInstance.interceptors.request.use(config => {
  const token = localStorage.getItem('token');
  if (token) config.headers!['Authorization'] = `Bearer ${token}`;
  return config;
});

export const fetchCurrentUser = async () => {
  try {
    return await axiosInstance.get('/users/me');
  } catch (error: any) {
    console.error('Erro ao buscar dados do usuário:', error.response?.status, error.response?.data);
    throw error;
  }
};

export const AuthProvider = ({ children }: AuthProviderProps) => {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem('@LinkGuardiao:token');
    if (token) {
      api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
      api.get('/users/me').then(response => {
        // Mapear os campos entre backend e frontend
        const userData = {
          id: response.data.id,
          name: response.data.username, // Backend envia username, frontend usa name
          email: response.data.email
        };
        setUser(userData);
      }).catch(error => {
        console.error('Erro ao buscar dados do usuário:', error);
        localStorage.removeItem('@LinkGuardiao:token');
      }).finally(() => {
        setLoading(false);
      });
    } else {
      setLoading(false);
    }
  }, []);

  const signIn = async (email: string, password: string) => {
    try {
      const response = await api.post('/auth/login', { Email: email, Password: password });
      const { token, user: userData } = response.data;
      
      localStorage.setItem('@LinkGuardiao:token', token);
      api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
      
      console.log('Dados do usuário recebidos do backend:', userData);
      
      // Mapear os campos entre backend e frontend
      const mappedUser = {
        id: userData.id,
        name: userData.username, // Backend envia username, frontend usa name
        email: userData.email
      };
      
      setUser(mappedUser);
    } catch (error: any) {
      console.error('Erro ao fazer login:', error);
      
      if (error.response) {
        console.error('Detalhes do erro:', {
          status: error.response.status,
          data: error.response.data
        });
      }
      
      throw error;
    }
  };

  const signUp = async (name: string, email: string, password: string) => {
    try {
      console.log('Enviando dados para registro:', { Name: name, Email: email, Password: password });
      
      // Verifique a URL base da API
      console.log('API baseURL:', api.defaults.baseURL);
      
      // Adicione logs para mostrar a URL completa
      const registerUrl = `${api.defaults.baseURL}/auth/register`;
      console.log('URL completa de registro:', registerUrl);
      
      const response = await api.post('/auth/register', { 
        Name: name, 
        Email: email, 
        Password: password 
      });
      
      console.log('Resposta do servidor:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('Erro ao registrar:', error);
      
      // Log mais detalhado do erro
      if (error.response) {
        console.error('Resposta de erro:', {
          status: error.response.status,
          data: error.response.data,
          headers: error.response.headers
        });
      }
      
      throw error;
    }
  };

  const register = signUp;

  const signOut = () => {
    localStorage.removeItem('@LinkGuardiao:token');
    api.defaults.headers.common['Authorization'] = '';
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{
      user,
      loading,
      isAuthenticated: !!user,
      signIn,
      signUp,
      signOut,
      register
    }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};