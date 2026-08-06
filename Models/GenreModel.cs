namespace BooksProject.Models;

public class Genre
{
    public int Id {get; set;}
    public required string Name {get; set;}
    public ICollection<Book> Books { get; set; } = [];
    // public ICollection<Book> Books { get; set; } = [];
}
