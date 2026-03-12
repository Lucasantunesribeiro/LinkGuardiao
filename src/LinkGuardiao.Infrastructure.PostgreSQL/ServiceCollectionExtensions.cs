using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Infrastructure.PostgreSQL.Persistence;
using LinkGuardiao.Infrastructure.PostgreSQL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinkGuardiao.Infrastructure.PostgreSQL
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPostgreSQLInfrastructure(
            this IServiceCollection services,
            string connectionString)
        {
            services.AddDbContext<LinkGuardiaoDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<ILinkRepository, EfLinkRepository>();
            services.AddScoped<IUserRepository, EfUserRepository>();
            services.AddScoped<IAccessLogRepository, EfAccessLogRepository>();
            services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();

            return services;
        }
    }
}
