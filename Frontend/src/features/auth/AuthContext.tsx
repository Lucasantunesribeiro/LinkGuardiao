import { createContext, useContext, useEffect, useState } from 'react';
import { api } from '../../lib/api/client';
import { AuthResult, UserDto } from '../../lib/api/types';
import { TOKEN_STORAGE_KEY, REFRESH_TOKEN_STORAGE_KEY } from './constants';

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
  const hasStoredToken = Boolean(localStorage.getItem(TOKEN_STORAGE_KEY));
  const [user, setUser] = useState<UserDto | null>(null);
  const [loading, setLoading] = useState(hasStoredToken);

  useEffect(() => {
    if (!hasStoredToken) {
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
  }, [hasStoredToken]);

  const signIn = async (email: string, password: string) => {
    const response = await api.post<AuthResult>('/auth/login', { email, password });
    localStorage.setItem(TOKEN_STORAGE_KEY, response.data.token);
    if (response.data.refreshToken) {
      localStorage.setItem(REFRESH_TOKEN_STORAGE_KEY, response.data.refreshToken);
    }
    setUser(response.data.user);
  };

  const signUp = async (name: string, email: string, password: string) => {
    const response = await api.post<AuthResult>('/auth/register', { name, email, password });
    localStorage.setItem(TOKEN_STORAGE_KEY, response.data.token);
    if (response.data.refreshToken) {
      localStorage.setItem(REFRESH_TOKEN_STORAGE_KEY, response.data.refreshToken);
    }
    setUser(response.data.user);
  };

  const signOut = () => {
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_STORAGE_KEY);
    if (refreshToken) {
      api.post('/auth/logout', { refreshToken }).catch(() => {});
    }
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    localStorage.removeItem(REFRESH_TOKEN_STORAGE_KEY);
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
