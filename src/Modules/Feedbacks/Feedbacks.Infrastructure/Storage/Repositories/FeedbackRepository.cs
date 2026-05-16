using Microsoft.EntityFrameworkCore;
using Skylab.Feedbacks.Application.Abstractions.Storage;
using Skylab.Feedbacks.Application.Contracts;
using Skylab.Feedbacks.Domain.Entities;
using Skylab.Shared.Application.Contracts;

namespace Skylab.Feedbacks.Infrastructure.Storage.Repositories;

public sealed class FeedbackRepository : IFeedbackRepository
{
    private readonly FeedbacksDbContext _context;

    public FeedbackRepository(FeedbacksDbContext context)
    {
        _context = context;
    }

    public Task<Feedback?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Feedbacks.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<PagedResult<FeedbackContract>> GetPagedAsync(GetFeedbacksRequest request, CancellationToken ct = default)
    {
        var query = _context.Feedbacks.AsNoTracking().AsQueryable();

        if (request.Topic.HasValue)
            query = query.Where(f => f.Topic == request.Topic.Value);

        if (request.Source.HasValue)
            query = query.Where(f => f.Source == request.Source.Value);

        if (request.SentTo.HasValue)
            query = query.Where(f => f.SentTo == request.SentTo.Value);

        query = request.SortDirection?.ToLower() == "ascending"
            ? query.OrderBy(f => f.CreatedAt)
            : query.OrderByDescending(f => f.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var feedbacks = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(f => new FeedbackContract(
                f.Id,
                f.Topic,
                f.Content,
                f.SentBy,
                f.Source,
                f.SentTo,
                f.CreatedAt
            ))
            .ToListAsync(ct);

        return new PagedResult<FeedbackContract>(feedbacks, totalCount, request.Page, request.PageSize);
    }

    public void Add(Feedback feedback) => _context.Feedbacks.Add(feedback);
}
