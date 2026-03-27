using Skylab.Feedbacks.Application.Contracts;
using Skylab.Feedbacks.Domain.Entities;
using Skylab.Feedbacks.Infrastructure.Storage;
using Skylab.Shared.Application.Contracts;
using Skylab.Shared.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Skylab.Feedbacks.Application.Services;

public class FeedbackService : IFeedbackService
{
    private readonly FeedbacksDbContext _context;

    public FeedbackService(FeedbacksDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<FeedbackContract>> CreateFeedbackAsync(CreateFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return new ServiceResult<FeedbackContract>(ServiceStatus.NotAcceptable, Message: "İçerik boş olamaz.");

        var feedback = new Feedback
        {
            Topic = request.Topic,
            Content = request.Content,
            SentBy = request.SentBy,
            Source = request.Source,
            SentTo = request.SentTo
        };

        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync(cancellationToken);

        return new ServiceResult<FeedbackContract>(ServiceStatus.Created, ToContract(feedback));
    }

    public async Task<ServiceResult<PagedResult<FeedbackContract>>> GetFeedbacksAsync(GetFeedbacksRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.Feedbacks.AsNoTracking().AsQueryable();

        if (request.Topic.HasValue)
            query = query.Where(f => f.Topic == request.Topic.Value);

        if (request.Source.HasValue)
            query = query.Where(f => f.Source == request.Source.Value);

        if (request.SentTo.HasValue)
            query = query.Where(f => f.SentTo == request.SentTo.Value);

        if (request.SortDirection?.ToLower() == "ascending")
            query = query.OrderBy(f => f.CreatedAt);
        else
            query = query.OrderByDescending(f => f.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var feedbacks = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(f => ToContract(f))
            .ToListAsync(cancellationToken);

        var result = new PagedResult<FeedbackContract>(feedbacks, totalCount, request.Page, request.PageSize);

        return new ServiceResult<PagedResult<FeedbackContract>>(ServiceStatus.Success, result);
    }

    public async Task<ServiceResult<FeedbackContract>> GetFeedbackByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var feedback = await _context.Feedbacks
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (feedback is null)
            return new ServiceResult<FeedbackContract>(ServiceStatus.NotFound, Message: "Feedback bulunamadı.");

        return new ServiceResult<FeedbackContract>(ServiceStatus.Success, ToContract(feedback));
    }

    private static FeedbackContract ToContract(Feedback f) => new(
        f.Id,
        f.Topic,
        f.Content,
        f.SentBy,
        f.Source,
        f.SentTo,
        f.CreatedAt
    );
}