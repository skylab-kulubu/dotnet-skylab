using Skylab.Forms.Domain.Models;
using Skylab.Forms.Domain.Enums;

namespace Skylab.Forms.Application.Contracts.Draft;

public record FormDraftContract(
    string Title,
    string? Description,
    List<FormSchemaItem> Schema,
    bool AllowAnonymousResponses,
    bool AllowMultipleResponses,
    bool RequiresManualReview,
    FormStatus Status,
    DateTime SavedAt
);

public record FormDraftRequest(
    Guid FormId,
    FormDraftContract Data
);