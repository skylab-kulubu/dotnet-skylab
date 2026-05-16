using Microsoft.EntityFrameworkCore;
using Skylab.Forms.Application.Abstractions.Storage;
using Skylab.Forms.Application.Contracts.Responses;
using Skylab.Forms.Domain.Entities;
using Skylab.Forms.Domain.Enums;

namespace Skylab.Forms.Infrastructure.Storage.Repositories;

public sealed class FormResponseRepository : IFormResponseRepository
{
    private readonly FormsDbContext _context;

    public FormResponseRepository(FormsDbContext context)
    {
        _context = context;
    }

    public Task<FormResponse?> GetLatestForUserAsync(Guid formId, Guid userId, CancellationToken ct = default) =>
        _context.Responses.AsNoTracking()
            .Where(r => r.FormId == formId && r.UserId == userId && !r.IsArchived)
            .OrderByDescending(r => r.SubmittedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<FormResponseCounts> GetCountsAsync(Guid formId, CancellationToken ct = default)
    {
        var result = await _context.Responses.AsNoTracking()
            .Where(r => r.FormId == formId)
            .GroupBy(_ => 1)
            .Select(g => new FormResponseCounts(
                g.Count(),
                g.Count(r => r.Status == FormResponseStatus.Pending),
                g.Average(r => (double?)r.TimeSpent)
            ))
            .FirstOrDefaultAsync(ct);

        return result ?? new FormResponseCounts(0, 0, null);
    }

    public Task<bool> HasNonArchivedResponseAsync(Guid formId, Guid userId, CancellationToken ct = default) =>
        _context.Responses.AsNoTracking()
            .AnyAsync(r => r.FormId == formId && r.UserId == userId && !r.IsArchived, ct);

    public Task<FormResponse?> GetByIdWithFormAndCollaboratorsAsync(Guid responseId, CancellationToken ct = default) =>
        _context.Responses.AsNoTracking()
            .Include(r => r.Form)
            .ThenInclude(f => f.Collaborators)
            .FirstOrDefaultAsync(r => r.Id == responseId, ct);

    public Task<FormResponse?> GetForEditByIdWithFormAndCollaboratorsAsync(Guid responseId, CancellationToken ct = default) =>
        _context.Responses
            .Include(r => r.Form)
            .ThenInclude(f => f.Collaborators)
            .FirstOrDefaultAsync(r => r.Id == responseId, ct);

    public async Task<PagedResponsesProjection> GetPagedAsync(Guid formId, GetResponsesRequest request, CancellationToken ct = default)
    {
        var query = _context.Responses.AsNoTracking().Where(r => r.FormId == formId);

        bool showArchived = request.ShowArchived.GetValueOrDefault(false);
        query = showArchived ? query.Where(r => r.IsArchived) : query.Where(r => !r.IsArchived);

        if (request.Status.HasValue)
            query = query.Where(r => r.Status == request.Status.Value);

        query = request.ResponderType switch
        {
            FormResponderType.Registered => query.Where(r => r.UserId != null),
            FormResponderType.Anonymous => query.Where(r => r.UserId == null),
            _ => query
        };

        if (request.FilterByUserId.HasValue)
            query = query.Where(r => r.UserId == request.FilterByUserId.Value);

        query = request.SortingDirection == "ascending"
            ? query.OrderBy(r => r.SubmittedAt)
            : query.OrderByDescending(r => r.SubmittedAt);

        var totalCount = await query.CountAsync(ct);
        var averageTimeSpent = await query.AverageAsync(r => r.TimeSpent, ct);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ResponseSummaryProjection(
                r.Id,
                r.UserId,
                r.Status,
                r.IsArchived,
                r.ReviewedBy,
                r.ArchivedBy,
                r.SubmittedAt,
                r.ReviewedAt,
                r.ArchivedAt
            ))
            .ToListAsync(ct);

        return new PagedResponsesProjection(items, totalCount, averageTimeSpent);
    }

    public async Task<IReadOnlyList<FormResponse>> GetNonArchivedByFormAsync(Guid formId, CancellationToken ct = default) =>
        await _context.Responses.AsNoTracking()
            .Where(r => r.FormId == formId && !r.IsArchived)
            .OrderBy(r => r.SubmittedAt)
            .ToListAsync(ct);

    public Task<Guid?> GetFirstChildResponseIdAsync(Guid childFormId, Guid userId, DateTime submittedAtOrAfter, CancellationToken ct = default) =>
        _context.Responses.AsNoTracking()
            .Where(r => r.FormId == childFormId && r.UserId == userId && r.SubmittedAt >= submittedAtOrAfter)
            .OrderBy(r => r.SubmittedAt)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(ct);

    public Task<Guid?> GetLatestParentResponseIdAsync(Guid parentFormId, Guid userId, DateTime submittedAtOrBefore, CancellationToken ct = default) =>
        _context.Responses.AsNoTracking()
            .Where(r => r.FormId == parentFormId && r.UserId == userId && r.SubmittedAt <= submittedAtOrBefore)
            .OrderByDescending(r => r.SubmittedAt)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(ct);

    public void Add(FormResponse response) => _context.Responses.Add(response);
}
