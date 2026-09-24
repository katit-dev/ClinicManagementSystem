namespace ClinicManagementSystem.Application.DTOs;


// =====================================================
// PAGED RESULT
// =====================================================

public class PagedResult<T>
{
    // =================================================
    // DATA
    // =================================================

    public List<T> Items { get; set; } = new();


    // =================================================
    // PAGINATION
    // =================================================

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}