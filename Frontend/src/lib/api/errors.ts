import axios from 'axios';

export interface ApiErrorPayload {
  message?: string;
  errors?: Record<string, string[]>;
}

export const getApiErrorMessage = (error: unknown, fallbackMessage: string) => {
  if (axios.isAxiosError<ApiErrorPayload>(error)) {
    return error.response?.data?.message ?? fallbackMessage;
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

export const hasApiResponse = (error: unknown) => {
  return axios.isAxiosError<ApiErrorPayload>(error) && Boolean(error.response);
};

export const hasApiRequest = (error: unknown) => {
  return axios.isAxiosError<ApiErrorPayload>(error) && Boolean(error.request);
};
