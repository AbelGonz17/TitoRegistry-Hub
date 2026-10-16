using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TitoRegistry.Domain.Entities;

namespace TitoRegistry.Application.Common.Interfaces;

/// <summary>
/// Contrato de abstracción para el contexto de base de datos de la aplicación.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<ServiceType> ServiceTypes { get; }
    DbSet<ServiceRecord> ServiceRecords { get; }
    DbSet<DriverSettlement> DriverSettlements { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
