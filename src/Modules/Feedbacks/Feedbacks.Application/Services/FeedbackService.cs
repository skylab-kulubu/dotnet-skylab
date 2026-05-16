using Skylab.Feedbacks.Application.Abstractions.Storage;
using Skylab.Feedbacks.Application.Contracts;
using Skylab.Feedbacks.Domain.Entities;
using Skylab.Shared.Application.Contracts;
using Skylab.Shared.Domain.Enums;

namespace Skylab.Feedbacks.Application.Services;

public class FeedbackService : IFeedbackService
{
    private readonly IFeedbackRepository _feedbacks;
    private readonly IFeedbacksUnitOfWork _uow;

    public FeedbackService(IFeedbackRepository feedbacks, IFeedbacksUnitOfWork uow)
    {
        _feedbacks = feedbacks;
        _uow = uow;
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

        _feedbacks.Add(feedback);
        await _uow.SaveChangesAsync(cancellationToken);

        return new ServiceResult<FeedbackContract>(ServiceStatus.Created, ToContract(feedback));
    }

    public async Task<ServiceResult<PagedResult<FeedbackContract>>> GetFeedbacksAsync(GetFeedbacksRequest request, CancellationToken cancellationToken = default)
    {
        var data = await _feedbacks.GetPagedAsync(request, cancellationToken);
        return new ServiceResult<PagedResult<FeedbackContract>>(ServiceStatus.Success, data);
    }

    public async Task<ServiceResult<FeedbackContract>> GetFeedbackByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var feedback = await _feedbacks.GetByIdAsync(id, cancellationToken);

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
