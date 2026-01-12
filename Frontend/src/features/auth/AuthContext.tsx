import { createContext, useContext, useEffect, useState } from 'react';
import { api } from '../../lib/api/client';
import { AuthResult, UserDto } from '../../lib/api/types';
import { TOKEN_STORAGE_KEY } from './constants';

interface AuthContextType {
  user: UserDto | null;
  loading: boolean;
  isAuthenticated: boolean;
  signIn: (email: string, password: string) => Promise<void>;
  signUp: (name: string, email: string, password: string) => Promise<void>;
  signOut: () => void;
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
  children: React.ReactNode;
}

export const AuthProvider = ({ children }: AuthProviderProps) => {
  const [user, setUser] = useState<UserDto | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem(TOKEN_STORAGE_KEY);
    if (!token) {
      setLoading(false);
      return;
    }

    api
      .get<UserDto>('/users/me')
      .then(response => {
        setUser(response.data);
      })
      .catch(() => {
        localStorage.removeItem(TOKEN_STORAGE_KEY);
      })
      .finally(() => {
        setLoading(false);
      });
  }, []);

  const signIn = async (email: string, password: string) => {
    const response = await api.post<AuthResult>('/auth/login', { email, password });
    localStorage.setItem(TOKEN_STORAGE_KEY, response.data.token);
    setUser(response.data.user);
  };

  const signUp = async (name: string, email: string, password: string) => {
    const response = await api.post<AuthResult>('/auth/register', { name, email, password });
    localStorage.setItem(TOKEN_STORAGE_KEY, response.data.token);
    setUser(response.data.user);
  };

  const signOut = () => {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    setUser(null);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        loading,
        isAuthenticated: !!user,
        signIn,
        signUp,
        signOut
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
