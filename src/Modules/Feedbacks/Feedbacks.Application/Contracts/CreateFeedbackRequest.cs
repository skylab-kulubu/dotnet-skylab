using Skylab.Feedbacks.Domain.Enums;

namespace Skylab.Feedbacks.Application.Contracts;

public record CreateFeedbackRequest(
    FeedbackTopic Topic,
    string Content,
    string? SentBy,
    FeedbackSource Source,
    FeedbackSentTo? SentTo
);