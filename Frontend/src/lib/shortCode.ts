// Mesmo formato aceito pelo backend (ShortCodePolicy.IsWellFormed): 1 a 32 caracteres [A-Za-z0-9_-].
// Validar antes de montar URLs impede que um caminho arbitrario (ex.: "../api/...")
// seja interpolado nas chamadas a API ou no redirect.
const SHORT_CODE_PATTERN = /^[A-Za-z0-9_-]{1,32}$/;

export const isWellFormedShortCode = (value: string | undefined): value is string =>
  typeof value === 'string' && SHORT_CODE_PATTERN.test(value);
