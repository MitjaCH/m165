using MongoDB.Bson.Serialization.Attributes;
namespace MovieApi;
public class Movie
{
    [BsonId] 
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public int Year { get; set; }
    public string Summary { get; set; } = "";
    public string[] Actors { get; set; } = Array.Empty<string>();
}