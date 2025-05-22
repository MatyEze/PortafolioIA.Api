using FastEndpoints;

namespace Api.Extensions;

public static class EndpointRegistration
{
    public static IApplicationBuilder UsePortfolioEndpoints(
        this IApplicationBuilder app)
    {
        // Middleware estándar (CORS, auth, excepciones, etc.)
        // app.UseCors("AllowAll");
        // app.UseAuthentication();
        // app.UseAuthorization();

        // FastEndpoints: registra todos los endpoints definidos
        app.UseFastEndpoints();

        return app;
    }
}