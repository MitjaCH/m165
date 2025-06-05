using Microsoft.Extensions.Options;
using MongoDB.Driver;
namespace MovieApi.Services;
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