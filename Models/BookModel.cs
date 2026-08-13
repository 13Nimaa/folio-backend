namespace BooksProject.Models;

public class Book
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    public decimal Price { get; set; }

    public decimal Rating { get; set; }

    public DateOnly PublishedDate { get; set; }

    // Book Information

    public required string Language { get; set; }



    // Inventory

    // Cover Image (base64)
public string? CoverImage { get; set; }
    // Author (plain string — no FK)
    public required string Author { get; set; }

    // Relationships
    public int GenreId { get; set; }
    public Genre Genre { get; set; } = null!;
}
