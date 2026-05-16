using Skylab.Forms.Application.Contracts.Forms;
using Skylab.Forms.Domain.Entities;
using Skylab.Shared.Application.Contracts;

namespace Skylab.Forms.Application.Abstractions.Storage;

public interface IFormRepository
{
    Task<Form?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Form?> GetWithCollaboratorsAsync(Guid id, CancellationToken ct = default);
    Task<Form?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<Form?> GetParentOfAsync(Guid childFormId, CancellationToken ct = default);
    Task<bool> IsChildFormAsync(Guid formId, CancellationToken ct = default);
    Task<bool> IsUserCollaboratorAsync(Guid formId, Guid userId, CancellationToken ct = default);

    Task<Form?> GetForEditWithCollaboratorsAsync(Guid id, CancellationToken ct = default);
    Task<Form?> GetForEditWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<Form?> GetForEditOwnedByAsync(Guid id, Guid ownerId, CancellationToken ct = default);
    Task<Form?> GetParentOfForEditAsync(Guid childFormId, CancellationToken ct = default);
    Task<bool> IsLinkedByAnotherFormAsync(Guid childFormId, Guid excludingParentFormId, CancellationToken ct = default);

    Task<PagedResult<FormSummaryContract>> GetUserFormsAsync(Guid userId, GetUserFormsRequest request, CancellationToken ct = default);
    Task<PagedResult<FormAllSummaryProjection>> GetAllFormsAsync(GetAllFormsRequest request, CancellationToken ct = default);
    Task<List<LinkableFormsContract>> GetLinkableFormsAsync(Guid currentFormId, Guid userId, CancellationToken ct = default);

    void Add(Form form);
}
