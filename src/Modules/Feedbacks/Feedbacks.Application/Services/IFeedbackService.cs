using Skylab.Feedbacks.Application.Contracts;
using Skylab.Shared.Application.Contracts;

namespace Skylab.Feedbacks.Application.Services;

public interface IFeedbackService
{
    Task<ServiceResult<FeedbackContract>> CreateFeedbackAsync(CreateFeedbackRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<FeedbackContract>>> GetFeedbacksAsync(GetFeedbacksRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<FeedbackContract>> GetFeedbackByIdAsync(Guid id, CancellationToken cancellationToken = default);
}