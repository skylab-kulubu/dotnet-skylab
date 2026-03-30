namespace Skylab.Forms.Application.Contracts.Forms;

public record GetAllFormsRequest(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? AllowAnonymous = null,
    bool? AllowMultiple = null,
    bool? RequiresManualReview = null,
    bool? HasLinkedForm = null,
    string SortDirection = "descending"
);
