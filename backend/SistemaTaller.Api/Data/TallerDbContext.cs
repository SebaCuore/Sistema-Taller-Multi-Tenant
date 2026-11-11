using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SistemaTaller.Api.Common;
using SistemaTaller.Api.Data.Entidades;

namespace SistemaTaller.Api.Data;

public class TallerDbContext(DbContextOptions<TallerDbContext> options, ITallerActual tallerActual)
    : DbContext(options)
{
    public DbSet<Taller> Talleres => Set<Taller>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<TipoVehiculo> TiposVehiculo => Set<TipoVehiculo>();
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<OrdenTrabajo> OrdenesTrabajo => Set<OrdenTrabajo>();
    public DbSet<OrdenTrabajoServicio> OrdenesTrabajoServicio => Set<OrdenTrabajoServicio>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Venta> Ventas => Set<Venta>();

    // Miembro del contexto al que apunta el filtro: EF lo trata como parámetro y lo reevalúa
    // en cada consulta. Si el filtro llevara el valor como constante, el modelo (que EF cachea)
    // quedaría fijado al taller del primer pedido.
    private Guid TallerIdActual => tallerActual.TallerId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TallerDbContext).Assembly);

        foreach (var entidad in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IPerteneceATaller).IsAssignableFrom(entidad.ClrType)) continue;

            // e => e.TallerId == this.TallerIdActual
            var parametro = Expression.Parameter(entidad.ClrType, "e");
            var propiedad = Expression.Property(parametro, nameof(IPerteneceATaller.TallerId));
            var valorActual = Expression.Property(Expression.Constant(this), nameof(TallerIdActual));
            var lambda = Expression.Lambda(Expression.Equal(propiedad, valorActual), parametro);

            modelBuilder.Entity(entidad.ClrType).HasQueryFilter("FiltroTaller", lambda);
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AsignarTaller();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        AsignarTaller();
        return base.SaveChanges();
    }

    private void AsignarTaller()
    {
        foreach (var entrada in ChangeTracker.Entries<IPerteneceATaller>())
        {
            if (entrada.State == EntityState.Added)
                entrada.Entity.TallerId = tallerActual.TallerId;
        }
    }
}
