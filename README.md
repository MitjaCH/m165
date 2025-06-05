# MongoDB WebAPI - Schritt-für-Schritt Prüfungsanleitung

## Schritt 1: Projekt Setup

### 1.1 Terminal öffnen und Projektverzeichnis erstellen
```bash
cd ~/Documents
mkdir min-api-with-mongo
cd min-api-with-mongo
```

### 1.2 .NET WebAPI Projekt erstellen
```bash
dotnet new web --name WebApi --framework net8.0
dotnet new gitignore
```

### 1.3 MongoDB Driver installieren
```bash
cd WebApi
dotnet add package MongoDB.Driver
```

### 1.4 Swagger installieren (falls gefordert)
```bash
dotnet add package Microsoft.AspNetCore.OpenApi
dotnet add package Swashbuckle.AspNetCore
```

### 1.5 launchSettings.json anpassen
**Datei:** `WebApi/Properties/launchSettings.json`
```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

## Schritt 2: DatabaseSettings erstellen

### 2.1 DatabaseSettings Klasse
**Neue Datei:** `WebApi/DatabaseSettings.cs`
```csharp
public class DatabaseSettings
{
    public string ConnectionString { get; set; } = "";
}
```

### 2.2 appsettings.json erweitern
**Datei:** `WebApi/appsettings.json`
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "DatabaseSettings": {
    "ConnectionString": "mongodb://gbs:geheim@localhost:27017"
  }
}
```

## Schritt 3: Movie Model erstellen

### 3.1 Movie Klasse
**Neue Datei:** `WebApi/Movie.cs`
```csharp
using MongoDB.Bson.Serialization.Attributes;

public class Movie
{
    [BsonId] 
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public int Year { get; set; }
    public string Summary { get; set; } = "";
    public string[] Actors { get; set; } = Array.Empty<string>();
}
```

## Schritt 4: MovieService erstellen

### 4.1 IMovieService Interface
**Neue Datei:** `WebApi/IMovieService.cs`
```csharp
public interface IMovieService
{
    IEnumerable<Movie> Get(); //get all movies
    Movie Get(string id); //movie by Id
    void Create(Movie movie); //create movie
    void Update(string id, Movie movie); //update movie
    void Delete(string id); //delete movie
}
```

### 4.2 MongoMovieService Implementation
**Neue Datei:** `WebApi/MongoMovieService.cs`
```csharp
using Microsoft.Extensions.Options;
using MongoDB.Driver;

public class MongoMovieService : IMovieService
{
    private readonly IMongoCollection<Movie> _movieCollection;
    private const string mongoDbDatabaseName = "gbs";
    private const string mongoDbCollectionName = "movies";

    // Constructor. Settings werden per dependency injection übergeben.
    public MongoMovieService(IOptions<DatabaseSettings> options)
    {
        var mongoDbConnectionString = options.Value.ConnectionString;
        var mongoClient = new MongoClient(mongoDbConnectionString);
        var database = mongoClient.GetDatabase(mongoDbDatabaseName);
        _movieCollection = database.GetCollection<Movie>(mongoDbCollectionName);
    }

    public void Create(Movie movie)
    {
        _movieCollection.InsertOne(movie);
    }

    public IEnumerable<Movie> Get()
    {
        return _movieCollection.Find(m => true).ToList();
    }

    public Movie Get(string id)
    {
        return _movieCollection.Find(m => m.Id == id).FirstOrDefault();
    }

    public void Update(string id, Movie movie)
    {
        _movieCollection.ReplaceOne(m => m.Id == id, movie);
    }

    public void Delete(string id)
    {
        _movieCollection.DeleteOne(m => m.Id == id);
    }
}
```

## Schritt 5: Program.cs komplett ersetzen

### 5.1 Program.cs vollständig überschreiben
**Datei:** `WebApi/Program.cs`
```csharp
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Configure DatabaseSettings
var movieDatabaseConfigSection = builder.Configuration.GetSection("DatabaseSettings");
builder.Services.Configure<DatabaseSettings>(movieDatabaseConfigSection);

// Register MovieService
builder.Services.AddSingleton<IMovieService, MongoMovieService>();

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Root endpoint
app.MapGet("/", () => "Minimal API Version 1.0");

// MongoDB connection check
app.MapGet("/check", (IOptions<DatabaseSettings> options) => {
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
```

## Schritt 6: MongoDB vorbereiten

### 6.1 MongoDB Container starten (falls noch nicht läuft)
```bash
docker run -d --name mongodb -p 27017:27017 -e MONGO_INITDB_ROOT_USERNAME=gbs -e MONGO_INITDB_ROOT_PASSWORD=geheim -v mongodb_data:/data/db mongo:latest
```

### 6.2 Ersten Test machen
```bash
# Im WebApi Verzeichnis
dotnet run
```

Browser öffnen: `http://localhost:5001/check`
Sollte zeigen: "Zugriff auf MongoDB ok. Vorhandene DBs: admin,config,local"

## Schritt 7: REST Client Tests einrichten

### 7.1 VS Code REST Client Extension installieren
In VS Code: Extensions → "REST Client" suchen und installieren

### 7.2 Testing-Datei erstellen
**Neue Datei:** `WebApi/testing.http`
```http
### Check MongoDB Connection
GET http://localhost:5001/check

### Get all movies
GET http://localhost:5001/api/movies

### Create new movie - Star Wars
POST http://localhost:5001/api/movies
Content-Type: application/json

{
    "id": "1",
    "title": "Star Wars",
    "year": 1977,
    "summary": "A new hope",
    "actors": ["Mark Hamill", "Harrison Ford"]
}

### Create new movie - The Matrix
POST http://localhost:5001/api/movies
Content-Type: application/json

{
    "id": "2",
    "title": "The Matrix",
    "year": 1999,
    "summary": "Reality is not what it seems",
    "actors": ["Keanu Reeves", "Laurence Fishburne"]
}

### Get movie by ID
GET http://localhost:5001/api/movies/1

### Update movie
PUT http://localhost:5001/api/movies/1
Content-Type: application/json

{
    "id": "1",
    "title": "Star Wars: Episode IV - A New Hope",
    "year": 1977,
    "summary": "Updated summary",
    "actors": ["Mark Hamill", "Harrison Ford", "Carrie Fisher"]
}

### Delete movie
DELETE http://localhost:5001/api/movies/2

### Test 404 - Non-existent movie
GET http://localhost:5001/api/movies/999
```

## Schritt 8: Schritt-für-Schritt Testen

### 8.1 Anwendung starten
```bash
# Terminal im WebApi Verzeichnis
dotnet run
```

### 8.2 Tests in genau dieser Reihenfolge ausführen:

1. **MongoDB Check:** Klick auf "Send Request" bei `GET http://localhost:5001/check`
2. **Alle Filme (leer):** `GET http://localhost:5001/api/movies` → sollte `[]` zurückgeben
3. **Film erstellen:** `POST http://localhost:5001/api/movies` (Star Wars)
4. **Film erstellen:** `POST http://localhost:5001/api/movies` (Matrix)
5. **Alle Filme:** `GET http://localhost:5001/api/movies` → sollte beide Filme zeigen
6. **Film by ID:** `GET http://localhost:5001/api/movies/1` → sollte Star Wars zeigen
7. **Film updaten:** `PUT http://localhost:5001/api/movies/1`
8. **Film löschen:** `DELETE http://localhost:5001/api/movies/2`
9. **404 Test:** `GET http://localhost:5001/api/movies/999` → sollte 404 geben

## Schritt 9: Swagger testen (optional)

### 9.1 Swagger UI öffnen
Browser: `http://localhost:5001/swagger`

### 9.2 Alle Endpunkte in Swagger testen
- GET /api/movies
- GET /api/movies/{id}
- POST /api/movies
- PUT /api/movies/{id}
- DELETE /api/movies/{id}

## Schritt 10: Projektstruktur überprüfen

Am Ende sollte deine Projektstruktur so aussehen:
```
min-api-with-mongo/
├── .gitignore
├── WebApi/
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── DatabaseSettings.cs
│   ├── Movie.cs
│   ├── IMovieService.cs
│   ├── MongoMovieService.cs
│   ├── Program.cs
│   ├── appsettings.json
│   ├── testing.http
│   └── WebApi.csproj
```

## Troubleshooting - Häufige Probleme

### Problem: MongoDB Verbindung fehlschlägt
**Lösung:**
```bash
# Prüfen ob MongoDB Container läuft
docker ps

# Falls nicht, starten:
docker start mongodb
```

### Problem: NuGet Package nicht gefunden
**Lösung:**
```bash
# Packages wiederherstellen
dotnet restore
```

### Problem: Port bereits in Verwendung
**Lösung:**
```bash
# Mit anderem Port starten
dotnet run --urls "http://localhost:5002"
```

### Problem: 404 bei allen Requests
**Prüfen:**
- Ist die Anwendung gestartet?
- Läuft sie auf Port 5001?
- Sind die URLs korrekt geschrieben?

### Problem: Movie wird nicht erstellt
**Prüfen:**
- Content-Type Header gesetzt?
- JSON korrekt formatiert?
- MongoDB Container läuft?

## Quick Reference - Wichtige Befehle

```bash
# Projekt starten
dotnet run

# MongoDB Container status
docker ps

# MongoDB Container starten
docker start mongodb

# Packages installieren
dotnet add package MongoDB.Driver

# VS Code öffnen
code .
```

## Prüfungstipps

1. **Immer zuerst /check testen** - damit weißt du, ob MongoDB läuft
2. **Schrittweise vorgehen** - erst GET all (leer), dann POST, dann GET all (mit Daten)
3. **Error Handling beachten** - 404 für nicht gefundene IDs testen
4. **JSON Format** - immer Content-Type: application/json setzen
5. **IDs verwenden** - bei POST immer eine ID mitgeben
6. **Status Codes prüfen** - 200 OK, 404 Not Found, etc.
