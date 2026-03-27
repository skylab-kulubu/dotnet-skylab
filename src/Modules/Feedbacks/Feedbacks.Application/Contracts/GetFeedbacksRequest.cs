using Skylab.Feedbacks.Domain.Enums;

namespace Skylab.Feedbacks.Application.Contracts;

public record GetFeedbacksRequest(
    int Page = 1,
    int PageSize = 10,
    FeedbackTopic? Topic = null,
    FeedbackSource? Source = null,
    FeedbackSentTo? SentTo = null,
    string SortDirection = "descending"
);