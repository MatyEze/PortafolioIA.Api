using Infrastructure.Data;
using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using FastEndpoints;
using Application.Interfaces;
using Infrastructure.Repositories;
using Api.Mapping;

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

        // 3) FluentValidation - FastEndpoints lo registrará automáticamente
        // Los validators en Api/Validators/ serán encontrados automáticamente

        // 4) Mapster
        services.AddMapster();

        // Configurar mappings de Mapster
        MappingConfig.RegisterMappings();

        // 5) Repositorios
        services.AddScoped<IDataPointRepository, DataPointRepository>();


        return services;
    }
}