using System.Diagnostics.CodeAnalysis;
using GameOfLife.Api.Data;
using GameOfLife.Api.Service;

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

        // Dependency Injection: wire each layer to its interface.
        //   - The file-IO pass-through and repository are Singletons: one shared store.
        //   - Service is Scoped: per-request; holds no long-lived state of its own.
        builder.Services.AddSingleton<IFileIOPassThrough, FileIOPassThrough>();
        builder.Services.AddSingleton<IBoardRepository>(serviceProvider =>
            new BoardRepositoryUsingFileSystem(
                serviceProvider.GetRequiredService<IFileIOPassThrough>(),
                Path.Combine(builder.Environment.ContentRootPath, "boarddata", "boards.json")));
        // The stepper is stateless pure domain logic, so one shared Singleton is safe.
        builder.Services.AddSingleton<IBoardStepper, BoardStepper>();
        builder.Services.AddScoped<IBoardService, BoardService>();

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
