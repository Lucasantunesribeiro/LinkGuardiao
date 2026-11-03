import axios from 'axios';

export interface ApiErrorPayload {
  message?: string;
  errors?: Record<string, string[]>;
}

export const getApiErrorMessage = (error: unknown, fallbackMessage: string) => {
  if (axios.isAxiosError<ApiErrorPayload>(error)) {
    const data = error.response?.data;
    // Erros de validacao (400) vem em `errors`; usa a primeira mensagem especifica se nao houver `message`.
    const firstValidationMessage = data?.errors ? Object.values(data.errors).flat()[0] : undefined;
    return data?.message ?? firstValidationMessage ?? fallbackMessage;
  }

  if (error instanceof Error && error.message) {
    return error.message;
  }

  return fallbackMessage;
};

export const getApiValidationErrors = (error: unknown) => {
  if (axios.isAxiosError<ApiErrorPayload>(error)) {
    return error.response?.data?.errors;
  }

  return undefined;
};

/** Primeira mensagem de validacao do backend para um campo (nome sem diferenciar maiusculas). */
export const getApiFieldError = (error: unknown, field: string) => {
  const errors = getApiValidationErrors(error);
  if (!errors) {
    return undefined;
  }

  const key = Object.keys(errors).find(name => name.toLowerCase() === field.toLowerCase());
  return key ? errors[key]?.[0] : undefined;
};

export const hasApiResponse = (error: unknown) => {
  return axios.isAxiosError<ApiErrorPayload>(error) && Boolean(error.response);
};

export const hasApiRequest = (error: unknown) => {
  return axios.isAxiosError<ApiErrorPayload>(error) && Boolean(error.request);
};
