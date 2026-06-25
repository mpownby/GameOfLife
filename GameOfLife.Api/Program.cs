using System.Diagnostics.CodeAnalysis;

// This is the composition root: the only place that wires concrete implementations
// to their interfaces. It is infrastructure glue, not business logic, so it is
// excluded from coverage rather than unit tested.
[ExcludeFromCodeCoverage]
internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // ---------------------------------------------------------------------
        // TODO (you write these): Dependency Injection wiring for each layer.
        // Register interfaces -> implementations here once you've created them, e.g.:
        //
        //   builder.Services.AddSingleton<IBoardRepository, FileBoardRepository>();
        //   builder.Services.AddScoped<IGameOfLifeService, GameOfLifeService>();
        //
        // Lifetime guidance:
        //   - The persistence/repository is typically a Singleton (shared store).
        //   - The service layer is usually Scoped (per-request) unless it holds no state.
        // ---------------------------------------------------------------------

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        // Health check endpoint
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        Console.WriteLine("Starting Game of Life API...");
        Console.WriteLine("  Swagger UI: /swagger");
        Console.WriteLine("  GET /health - Health check");

        app.Run();
    }
}
