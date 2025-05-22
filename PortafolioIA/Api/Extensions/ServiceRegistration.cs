using Infrastructure.Data;
using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using FastEndpoints;
using Application.Interfaces;
using Infrastructure.Repositories;

namespace Api.Extensions;

public static class ServiceRegistration
{
    public static IServiceCollection AddPortfolioServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1) EF Core con PostgreSQL (Supabase)
        services.AddDbContext<PortfolioDbContext>(opts =>
            opts.UseNpgsql(
                configuration.GetConnectionString("SupaBasePostgres")));

        // 2) FastEndpoints
        services.AddFastEndpoints();

        // 3) FluentValidation
        services.AddValidatorsFromAssembly(typeof(Program).Assembly);

        // 4) Mapster (registro de mapeos)
        services.AddMapster();

        // 5) Repositorios y servicios de Infraestructura
        services.AddScoped<IDataPointRepository, DataPointRepository>();
        // TODO: Agregar más repositorios (IActivoRepository, IDivisaRepository, etc.)

        return services;
    }
}