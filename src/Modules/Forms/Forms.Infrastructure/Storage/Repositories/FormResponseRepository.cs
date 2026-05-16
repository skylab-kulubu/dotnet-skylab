using Microsoft.EntityFrameworkCore;
using Skylab.Forms.Application.Abstractions.Storage;
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
}
