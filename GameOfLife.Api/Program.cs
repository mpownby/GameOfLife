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
        builder.Services.AddSwaggerGen(options =>
        {
            // Pick up the inline [SwaggerOperation]/[SwaggerResponse] attributes on the controllers.
            options.EnableAnnotations();
        });

        // Dependency Injection: wire each layer to its interface. Everything here is a Singleton:
        //   - The file-IO pass-through and repository: one shared store.
        //   - The stepper: stateless pure domain logic.
        //   - The service: stateless apart from the lock it uses to serialize each board's
        //     read-modify-write. That lock must be shared across all requests to mean anything, so
        //     the service has to be a singleton (a scoped instance-per-request lock would protect
        //     nothing). It's safe because the service holds no per-request state.
        builder.Services.AddSingleton<IFileIOPassThrough, FileIOPassThrough>();
        builder.Services.AddSingleton<IBoardRepository>(serviceProvider =>
            new BoardRepositoryUsingFileSystem(
                serviceProvider.GetRequiredService<IFileIOPassThrough>(),
                Path.Combine(builder.Environment.ContentRootPath, "boarddata", "boards.json")));
        builder.Services.AddSingleton<IBoardStepper, BoardStepper>();
        builder.Services.AddSingleton<IBoardService, BoardService>();

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseHttpsRedirection();
        app.MapControllers();

        // Health check endpoint
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        Console.WriteLine("Starting Game of Life API...");
        Console.WriteLine("  Swagger UI: /swagger");
        Console.WriteLine("  GET /health - Health check");

        app.Run();
    }
}
