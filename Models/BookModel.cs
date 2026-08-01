
using BooksProject.Models;

namespace BooksProject.Models;

public class Book
{
    public int id {get; set;}

    public required string Title {get; set;}

    public  string Description {get; set;}
    public decimal Price {get; set ;}
    public DateOnly PublishedDate {get; set;}

    public int GenreId {get; set;}
}