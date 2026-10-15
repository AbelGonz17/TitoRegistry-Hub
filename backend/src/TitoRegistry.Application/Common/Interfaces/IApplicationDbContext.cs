using Microsoft.EntityFrameworkCore.Infrastructure;

namespace TitoRegistry.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
