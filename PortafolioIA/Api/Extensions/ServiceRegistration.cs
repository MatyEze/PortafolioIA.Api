using Infrastructure.Data;
using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using FastEndpoints;
using Application.Interfaces;
using Infrastructure.Repositories;
using Infrastructure.Parsing;
using Api.Mapping;

namespace Api.Extensions;

public static class ServiceRegistration
{
    public static IServiceCollection AddPortfolioServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1) EF Core con PostgreSQL (Supabase) - Password desde secrets/env vars
        var baseConnectionString = configuration.GetConnectionString("SupaBasePostgres");
        var password = configuration["Database:Password"];

        if (string.IsNullOrEmpty(baseConnectionString))
            throw new InvalidOperationException("ConnectionString 'SupaBasePostgres' no configurado");

        if (string.IsNullOrEmpty(password))
            throw new InvalidOperationException("Database:Password no configurado en secrets o variables de entorno");

        var fullConnectionString = $"{baseConnectionString};Password={password}";

        services.AddDbContext<PortfolioDbContext>(opts =>
            opts.UseNpgsql(fullConnectionString));

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

        // 6) Servicios de parsing - Registrar parsers individuales primero
        services.AddScoped<IOLExcelParser>();

        // Luego registrar el factory que los consume
        services.AddScoped<IFileParsingService>(provider =>
        {
            var parsers = new List<IFileParsingService>
            {
                provider.GetRequiredService<IOLExcelParser>()
                // Agregar más parsers aquí en el futuro
                // provider.GetRequiredService<BalanzParser>(),
                // provider.GetRequiredService<BullParser>()
            };
            return new ParsingServiceFactory(parsers);
        });

        return services;
    }
}