namespace MovieApi.Services;
public interface IMovieService
{
    IEnumerable<Movie> Get(); //get all movies
    Movie Get(string id); //movie by Id
    void Create(Movie movie); //create movie
    void Update(string id, Movie movie); //update movie
    void Delete(string id); //delete movie
}