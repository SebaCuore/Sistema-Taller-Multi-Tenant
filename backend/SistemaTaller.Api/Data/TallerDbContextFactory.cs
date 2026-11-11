using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SistemaTaller.Api.Common;

namespace SistemaTaller.Api.Data;

/// <summary>Solo para <c>dotnet ef</c>: arma el contexto sin necesidad de un usuario autenticado.</summary>
public class TallerDbContextFactory : IDesignTimeDbContextFactory<TallerDbContext>
{
    public TallerDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<TallerDbContextFactory>()
            .AddEnvironmentVariables()
            .Build();
        var cadena = config.GetConnectionString("Default")
            ?? "Host=localhost;Database=sistema_taller;Username=postgres;Password=devpassword";

        var opciones = new DbContextOptionsBuilder<TallerDbContext>().UseNpgsql(cadena).Options;
        return new TallerDbContext(opciones, new TallerFijo(Guid.Empty));
    }
}
