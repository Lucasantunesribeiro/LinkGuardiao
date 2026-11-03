import { AxiosError, AxiosResponse } from 'axios';
import { describe, expect, it } from 'vitest';
import { getApiErrorMessage, getApiFieldError } from './errors';

const validationError = (errors: Record<string, string[]>) =>
  new AxiosError('Request failed', 'ERR_BAD_REQUEST', undefined, undefined, {
    status: 400,
    data: { title: 'Validation error', errors },
  } as AxiosResponse);

describe('api errors', () => {
  it('usa a mensagem de validacao do backend em vez da mensagem generica', () => {
    const error = validationError({ OriginalUrl: ['O endereço aponta para um IP privado, de loopback ou reservado.'] });

    expect(getApiErrorMessage(error, 'Erro genérico')).toBe(
      'O endereço aponta para um IP privado, de loopback ou reservado.'
    );
  });

  it('encontra o erro do campo sem diferenciar maiusculas', () => {
    const error = validationError({ OriginalUrl: ['Apenas URLs http e https são permitidas.'] });

    expect(getApiFieldError(error, 'originalUrl')).toBe('Apenas URLs http e https são permitidas.');
    expect(getApiFieldError(error, 'title')).toBeUndefined();
  });

  it('cai no fallback quando nao ha resposta', () => {
    expect(getApiErrorMessage(new AxiosError('Network Error'), 'Sem conexão')).toBe('Sem conexão');
  });
});
