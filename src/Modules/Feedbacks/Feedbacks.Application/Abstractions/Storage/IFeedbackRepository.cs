using Skylab.Feedbacks.Application.Contracts;
using Skylab.Feedbacks.Domain.Entities;
using Skylab.Shared.Application.Contracts;

namespace Skylab.Feedbacks.Application.Abstractions.Storage;

public interface IFeedbackRepository
{
    Task<Feedback?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<FeedbackContract>> GetPagedAsync(GetFeedbacksRequest request, CancellationToken ct = default);

    void Add(Feedback feedback);
}
