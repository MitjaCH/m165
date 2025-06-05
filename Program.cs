using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MovieApi.Services;

namespace MovieApi;
class Program
{
    static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure DatabaseSettings
        var movieDatabaseConfigSection = builder.Configuration.GetSection("DatabaseSettings");
        builder.Services.Configure<DatabaseSettings>(movieDatabaseConfigSection);

        // Register MovieService
        builder.Services.AddSingleton<IMovieService, MongoMovieService>();


        var app = builder.Build();
        // Root endpoint
        app.MapGet("/", () => "Minimal API Version 1.0");

        // MongoDB connection check
        app.MapGet("/check", (IOptions<DatabaseSettings> options) =>
        {
            try
            {
                var mongoDbConnectionString = options.Value.ConnectionString;
                var mongoClient = new MongoDB.Driver.MongoClient(mongoDbConnectionString);
                var databases = mongoClient.ListDatabaseNames().ToList();
                return $"Zugriff auf MongoDB ok. Vorhandene DBs: {string.Join(",", databases)}";
            }
            catch (Exception ex)
            {
                return $"Fehler beim Zugriff auf MongoDB: {ex.Message}";
            }
        });

        // Get all Movies
        app.MapGet("/api/movies", (IMovieService movieService) =>
        {
            return Results.Ok(movieService.Get());
        });

        // Get Movie by id
        app.MapGet("/api/movies/{id}", (IMovieService movieService, string id) =>
        {
            var movie = movieService.Get(id);
            return movie != null
                ? Results.Ok(movie)
                : Results.NotFound();
        });

        // Insert Movie
        app.MapPost("/api/movies", (IMovieService movieService, Movie movie) =>
        {
            movieService.Create(movie);
            return Results.Ok(movie);
        });

        // Update Movie
        app.MapPut("/api/movies/{id}", (IMovieService movieService, string id, Movie movie) =>
        {
            var existingMovie = movieService.Get(id);
            if (existingMovie == null)
            {
                return Results.NotFound();
            }

            movie.Id = id;
            movieService.Update(id, movie);
            return Results.Ok(movie);
        });

        // Delete Movie
        app.MapDelete("/api/movies/{id}", (IMovieService movieService, string id) =>
        {
            var movie = movieService.Get(id);
            if (movie == null)
            {
                return Results.NotFound();
            }

            movieService.Delete(id);
            return Results.Ok();
        });

        app.Run();
    }
}
