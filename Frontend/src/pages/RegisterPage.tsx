import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../features/auth/AuthContext';
import { toast } from 'react-toastify';
import { FiUser, FiMail, FiLock } from 'react-icons/fi';
import Input from '../components/Input';

const RegisterPage = () => {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errors, setErrors] = useState<{name?: string, email?: string, password?: string, confirmPassword?: string}>({});
  const { signUp } = useAuth();
  const navigate = useNavigate();

  const validateForm = () => {
    const newErrors: {name?: string, email?: string, password?: string, confirmPassword?: string} = {};
    let isValid = true;

    if (!name.trim()) {
      newErrors.name = 'O nome é obrigatório';
      isValid = false;
    }

    if (!email.trim()) {
      newErrors.email = 'O email é obrigatório';
      isValid = false;
    } else if (!/\S+@\S+\.\S+/.test(email)) {
      newErrors.email = 'E-mail inválido';
      isValid = false;
    }

    if (!password) {
      newErrors.password = 'A senha é obrigatória';
      isValid = false;
    } else if (password.length < 6) {
      newErrors.password = 'A senha deve ter pelo menos 6 caracteres';
      isValid = false;
    }

    if (password !== confirmPassword) {
      newErrors.confirmPassword = 'As senhas não coincidem';
      isValid = false;
    }

    setErrors(newErrors);
    return isValid;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) {
      const errorMessages = Object.values(errors).filter(Boolean);
      if (errorMessages.length > 0) {
        toast.error(errorMessages[0]);
      }
      return;
    }
    
    setIsSubmitting(true);
    
    try {
      await signUp(name, email, password);
      toast.success('Registro realizado com sucesso! Faça login para continuar.');
      navigate('/login');
    } catch (error: any) {
      if (error.response) {
        const message = error.response.data?.message || 'Erro ao registrar. Tente novamente.';
        toast.error(message);
        
        if (error.response.status === 400 && error.response.data.errors) {
          const serverErrors = error.response.data.errors;
          const newErrors: any = {};
          
          Object.keys(serverErrors).forEach(key => {
            const formattedKey = key.charAt(0).toLowerCase() + key.slice(1);
            newErrors[formattedKey] = serverErrors[key][0];
          });
          setErrors(newErrors);
        }
      } else if (error.request) {
        toast.error('Não foi possível conectar ao servidor. Verifique sua conexão.');
      } else {
        toast.error('Erro ao configurar a requisição: ' + error.message);
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="grid gap-10 lg:grid-cols-2 items-center">
      <div className="space-y-6">
        <span className="badge">Comece agora</span>
        <h1 className="text-4xl md:text-5xl leading-tight">
          Crie sua conta e tenha controle total dos seus links.
        </h1>
        <p className="text-stone-600">
          Crie links protegidos, controle expiracao e veja tudo o que acontece com suas campanhas.
        </p>
        <div className="grid gap-3 text-sm text-stone-500">
          <div className="panel px-4 py-3">Dashboard com estatisticas em tempo real</div>
          <div className="panel px-4 py-3">Links com senha e expiração automatica</div>
        </div>
      </div>

      <div className="panel p-8 shadow-soft-xl">
        <div className="space-y-2">
          <h2 className="text-2xl font-display">Criar conta</h2>
          <p className="text-sm text-stone-500">
            Ou{' '}
            <Link to="/login" className="link font-medium">
              fazer login
            </Link>
          </p>
        </div>
        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div className="rounded-md space-y-4">
            <Input
              id="name"
              name="name"
              type="text"
              label="Nome"
              icon={FiUser}
              placeholder="Seu nome"
              value={name}
              onChange={(e) => setName(e.target.value)}
              error={errors.name}
              required
            />
            <Input
              id="email"
              name="email"
              type="email"
              label="Email"
              icon={FiMail}
              placeholder="voce@empresa.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              error={errors.email}
              required
            />
            <Input
              id="password"
              name="password"
              type="password"
              label="Senha"
              icon={FiLock}
              placeholder="Minimo de 6 caracteres"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              error={errors.password}
              required
            />
            <Input
              id="confirmPassword"
              name="confirmPassword"
              type="password"
              label="Confirmar senha"
              icon={FiLock}
              placeholder="Repita sua senha"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              error={errors.confirmPassword}
              required
            />
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="btn-secondary w-full flex justify-center items-center"
          >
            {isSubmitting ? 'Processando...' : 'Criar conta'}
          </button>
        </form>
      </div>
    </div>
  );
};

export default RegisterPage;
