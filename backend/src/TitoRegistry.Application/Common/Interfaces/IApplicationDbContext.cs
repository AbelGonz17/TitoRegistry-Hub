using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TitoRegistry.Domain.Entities;

namespace TitoRegistry.Application.Common.Interfaces;

/// <summary>
/// Contrato de abstracción para el acceso a datos en la capa Application.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Employee> Employees { get; }
    DbSet<ServiceType> ServiceTypes { get; }
    DbSet<ServiceRecord> ServiceRecords { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
