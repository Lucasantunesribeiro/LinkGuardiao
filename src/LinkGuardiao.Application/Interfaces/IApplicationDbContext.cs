using LinkGuardiao.Application.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkGuardiao.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; }
        DbSet<ShortenedLink> ShortenedLinks { get; }
        DbSet<LinkAccess> LinkAccesses { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
