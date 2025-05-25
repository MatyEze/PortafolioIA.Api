using FastEndpoints;

namespace Api.Extensions;

public static class EndpointRegistration
{
    public static IApplicationBuilder UsePortfolioEndpoints(
        this IApplicationBuilder app)
    {
        // CORS si es necesario
        // app.UseCors("AllowAll");

        // Authentication/Authorization si es necesario
        // app.UseAuthentication();
        // app.UseAuthorization();

        // FastEndpoints ya está configurado en Program.cs
        // Este método queda para futuras configuraciones de endpoints

        return app;
    }
}