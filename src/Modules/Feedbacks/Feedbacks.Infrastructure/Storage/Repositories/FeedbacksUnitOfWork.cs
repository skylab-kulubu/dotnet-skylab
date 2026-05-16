using Skylab.Feedbacks.Application.Abstractions.Storage;

namespace Skylab.Feedbacks.Infrastructure.Storage.Repositories;

public sealed class FeedbacksUnitOfWork : IFeedbacksUnitOfWork
{
    private readonly FeedbacksDbContext _context;

    public FeedbacksUnitOfWork(FeedbacksDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
}
