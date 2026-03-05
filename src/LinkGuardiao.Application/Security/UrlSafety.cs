namespace LinkGuardiao.Application.Security
{
    public static class UrlSafety
    {
        private static readonly HashSet<string> AllowedSchemes = new(StringComparer.OrdinalIgnoreCase)
        {
            "http",
            "https"
        };

        public static bool IsSafeHttpUrl(string? value, out Uri? uri)
        {
            uri = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (value.Length > 2048)
            {
                return false;
            }

            if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var parsed))
            {
                return false;
            }

            if (!AllowedSchemes.Contains(parsed.Scheme))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(parsed.Host))
            {
                return false;
            }

            uri = parsed;
            return true;
        }

        public static bool IsSafeHttpUrl(string? value)
        {
            return IsSafeHttpUrl(value, out _);
        }
    }
}
