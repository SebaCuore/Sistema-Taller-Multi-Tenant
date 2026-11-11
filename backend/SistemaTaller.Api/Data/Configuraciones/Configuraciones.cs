using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaTaller.Api.Data.Entidades;

namespace SistemaTaller.Api.Data.Configuraciones;

public class TallerConfiguration : IEntityTypeConfiguration<Taller>
{
    public static readonly Guid IdSanMartin = Guid.Parse("0b6a3c1e-5d0e-4f0a-9a52-6d7a1f0f3a01");

    public void Configure(EntityTypeBuilder<Taller> b)
    {
        b.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        b.HasData(new Taller
        {
            Id = IdSanMartin,
            Nombre = "San Martín",
            Activo = true,
            FechaAlta = new DateTime(2026, 11, 11, 0, 0, 0, DateTimeKind.Utc),
        });
    }
}

public class TipoVehiculoConfiguration : IEntityTypeConfiguration<TipoVehiculo>
{
    public static readonly Guid IdAuto = Guid.Parse("f8184fbd-3989-40c0-973a-0cc5bab89dab");
    public static readonly Guid IdMoto = Guid.Parse("6dece58d-f153-4c45-94cf-7c8b138a4879");

    public void Configure(EntityTypeBuilder<TipoVehiculo> b)
    {
        b.Property(x => x.Nombre).HasMaxLength(50).IsRequired();
        b.HasData(
            new TipoVehiculo { Id = IdAuto, Nombre = "Auto", Activo = true },
            new TipoVehiculo { Id = IdMoto, Nombre = "Moto", Activo = true });
    }
}

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        b.Property(x => x.Email).HasMaxLength(200).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(100).IsRequired();
        b.Property(x => x.Rol).HasConversion<string>().HasMaxLength(30);
        b.HasIndex(x => new { x.TallerId, x.Email }).IsUnique();
        b.HasOne<Taller>().WithMany().HasForeignKey(x => x.TallerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VehiculoConfiguration : IEntityTypeConfiguration<Vehiculo>
{
    public void Configure(EntityTypeBuilder<Vehiculo> b)
    {
        b.Property(x => x.Patente).HasMaxLength(10).IsRequired();
        b.Property(x => x.Marca).HasMaxLength(60);
        b.Property(x => x.Modelo).HasMaxLength(60);
        b.Property(x => x.ContactoNombre).HasMaxLength(120);
        b.Property(x => x.ContactoTelefono).HasMaxLength(40);
        b.HasIndex(x => new { x.TallerId, x.Patente });
        b.HasOne(x => x.TipoVehiculo).WithMany()
            .HasForeignKey(x => x.TipoVehiculoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Taller>().WithMany().HasForeignKey(x => x.TallerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ServicioConfiguration : IEntityTypeConfiguration<Servicio>
{
    // Catálogo por defecto. Los precios son los del prototipo: confirmar con el dueño real antes de producción.
    public void Configure(EntityTypeBuilder<Servicio> b)
    {
        b.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        b.Property(x => x.CategoriaVehiculo).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.PrecioReferencia).HasColumnType("numeric(12,2)");
        b.HasOne<Taller>().WithMany().HasForeignKey(x => x.TallerId).OnDelete(DeleteBehavior.Restrict);

        var t = TallerConfiguration.IdSanMartin;
        b.HasData(
            S("a1000000-0000-4000-8000-000000000001", t, "Alineación", CategoriaServicio.Auto, true, false, 32000m),
            S("a1000000-0000-4000-8000-000000000002", t, "Balanceo", CategoriaServicio.Ambos, true, true, 18000m),
            S("a1000000-0000-4000-8000-000000000003", t, "Reparación de neumático", CategoriaServicio.Ambos, true, true, 12000m),
            S("a1000000-0000-4000-8000-000000000004", t, "Cambio de neumáticos", CategoriaServicio.Ambos, true, true, null),
            S("a1000000-0000-4000-8000-000000000005", t, "Rotación", CategoriaServicio.Auto, false, false, null),
            S("a1000000-0000-4000-8000-000000000006", t, "Service de moto", CategoriaServicio.Moto, false, false, null),
            S("a1000000-0000-4000-8000-000000000007", t, "Mecánica general", CategoriaServicio.Ambos, false, false, null));
    }

    private static Servicio S(string id, Guid taller, string nombre, CategoriaServicio cat,
        bool posicion, bool lado, decimal? precio) => new()
    {
        Id = Guid.Parse(id), TallerId = taller, Nombre = nombre, CategoriaVehiculo = cat,
        AplicaPosicion = posicion, AplicaLado = lado, PrecioReferencia = precio, Activo = true,
    };
}

public class OrdenTrabajoConfiguration : IEntityTypeConfiguration<OrdenTrabajo>
{
    public void Configure(EntityTypeBuilder<OrdenTrabajo> b)
    {
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(30);
        b.HasIndex(x => new { x.TallerId, x.Estado });
        b.HasOne<Taller>().WithMany().HasForeignKey(x => x.TallerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Vehiculo>().WithMany().HasForeignKey(x => x.VehiculoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Servicios).WithOne()
            .HasForeignKey(x => x.OrdenTrabajoId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrdenTrabajoServicioConfiguration : IEntityTypeConfiguration<OrdenTrabajoServicio>
{
    public void Configure(EntityTypeBuilder<OrdenTrabajoServicio> b)
    {
        b.Property(x => x.Posicion).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Lado).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.PrecioFinal).HasColumnType("numeric(12,2)");
        b.HasOne<Taller>().WithMany().HasForeignKey(x => x.TallerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Servicio>().WithMany().HasForeignKey(x => x.ServicioId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> b)
    {
        b.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        b.Property(x => x.Precio).HasColumnType("numeric(12,2)");
        b.HasOne<Taller>().WithMany().HasForeignKey(x => x.TallerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> b)
    {
        b.Property(x => x.PrecioUnitario).HasColumnType("numeric(12,2)");
        b.HasOne<Taller>().WithMany().HasForeignKey(x => x.TallerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
    }
}
