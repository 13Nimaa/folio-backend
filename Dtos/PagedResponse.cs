namespace BooksProject.Dtos;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int CurrentPage,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);
