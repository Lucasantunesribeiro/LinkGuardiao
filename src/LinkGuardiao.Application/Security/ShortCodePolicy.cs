namespace LinkGuardiao.Application.Security
{
    /// <summary>
    /// Regras de formato para codigos curtos e lista de codigos reservados.
    /// Codigos reservados colidiriam com rotas da API (ex.: "/health") ou do frontend
    /// (ex.: "/login") e nunca devem ser gerados.
    /// </summary>
    public static class ShortCodePolicy
    {
        /// <summary>Tamanho maximo aceito na leitura (codigos legados migrados podem variar).</summary>
        public const int MaxLookupLength = 32;

        private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
        {
            "api", "r", "health", "swagger", "login", "logout", "signup", "signin", "register",
            "dashboard", "create", "create-link", "edit", "edit-link", "stats", "admin", "static",
            "assets", "public", "favicon", "robots", "sitemap", "index", "status", "metrics", "auth",
            "users", "links", "access", "grant", "null", "undefined"
        };

        public static bool IsReserved(string shortCode)
        {
            return Reserved.Contains(shortCode);
        }

        /// <summary>
        /// Formato aceito em rotas publicas: 1 a 32 caracteres [A-Za-z0-9_-].
        /// Rejeitar cedo evita consultas ao banco/cache com entrada arbitraria.
        /// </summary>
        public static bool IsWellFormed(string? shortCode)
        {
            if (string.IsNullOrEmpty(shortCode) || shortCode.Length > MaxLookupLength)
            {
                return false;
            }

            foreach (var c in shortCode)
            {
                if (!(char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_'))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
