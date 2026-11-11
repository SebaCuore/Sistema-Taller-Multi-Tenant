using Microsoft.EntityFrameworkCore;
using SistemaTaller.Api.Common;
using SistemaTaller.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITallerActual, TallerActual>();
builder.Services.AddDbContext<TallerDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
