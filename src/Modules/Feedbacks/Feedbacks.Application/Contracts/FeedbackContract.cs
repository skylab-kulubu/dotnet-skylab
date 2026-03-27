using Skylab.Feedbacks.Domain.Enums;

namespace Skylab.Feedbacks.Application.Contracts;

public record FeedbackContract(
    Guid Id,
    FeedbackTopic Topic,
    string Content,
    string? SentBy,
    FeedbackSource Source,
    FeedbackSentTo? SentTo,
    DateTime CreatedAt
);