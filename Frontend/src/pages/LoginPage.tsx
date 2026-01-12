import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../features/auth/AuthContext';
import { toast } from 'react-toastify';
import { FiMail, FiLock, FiEye, FiEyeOff } from 'react-icons/fi';
import Input from '../components/Input';
import Loading from '../components/Loading';

const LoginPage = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { signIn } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!email || !password) {
      toast.error('Por favor, preencha todos os campos');
      return;
    }
    
    setIsSubmitting(true);
    
    try {
      await signIn(email, password);
      toast.success('Login realizado com sucesso!');
      navigate('/dashboard');
    } catch (error: any) {
      const message = error.response?.data?.message || 'Erro ao fazer login. Verifique suas credenciais.';
      toast.error(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="grid gap-10 lg:grid-cols-2 items-center">
      <div className="space-y-6">
        <span className="badge">Acesso seguro</span>
        <h1 className="text-4xl md:text-5xl leading-tight">
          Entre e continue monitorando seus links.
        </h1>
        <p className="text-stone-600">
          Centralize campanhas, proteja acessos e acompanhe resultados com visao estrategica.
        </p>
        <div className="grid gap-3 text-sm text-stone-500">
          <div className="panel px-4 py-3">Controle de expiracao e senha</div>
          <div className="panel px-4 py-3">Insights diarios de performance</div>
        </div>
      </div>

      <div className="panel p-8 shadow-soft-xl">
        <div className="space-y-2">
          <h2 className="text-2xl font-display">Entrar</h2>
          <p className="text-sm text-stone-500">
            Ou{' '}
            <Link to="/register" className="link font-medium">
              crie sua conta gratuita
            </Link>
          </p>
        </div>

        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div className="rounded-md space-y-4">
            <Input
              id="email"
              name="email"
              type="email"
              label="Email"
              icon={FiMail}
              placeholder="voce@empresa.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              autoComplete="email"
              disabled={isSubmitting}
            />

            <Input
              id="password"
              name="password"
              type={showPassword ? 'text' : 'password'}
              label="Senha"
              icon={FiLock}
              rightIcon={showPassword ? FiEyeOff : FiEye}
              onRightIconClick={() => setShowPassword(!showPassword)}
              placeholder="Sua senha segura"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              autoComplete="current-password"
              disabled={isSubmitting}
            />
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="btn-primary w-full flex justify-center items-center space-x-2"
          >
            {isSubmitting ? (
              <>
                <Loading size="sm" />
                <span>Processando...</span>
              </>
            ) : (
              'Entrar'
            )}
          </button>
        </form>
      </div>
    </div>
  );
};

export default LoginPage;
