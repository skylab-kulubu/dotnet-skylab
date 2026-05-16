namespace Skylab.Feedbacks.Application.Abstractions.Storage;

public interface IFeedbacksUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
