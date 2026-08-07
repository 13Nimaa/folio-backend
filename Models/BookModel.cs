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
    public required string ISBN { get; set; }

    public required string Language { get; set; }

    public int Pages { get; set; }

    // Inventory
    public int StockQuantity { get; set; }

    // Cover Image
    public string? CoverImageUrl { get; set; }

    // Relationships
    public int GenreId { get; set; }
    public Genre Genre { get; set; } = null!;

    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;
}
