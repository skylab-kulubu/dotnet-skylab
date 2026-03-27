using Skylab.Feedbacks.Domain.Enums;
using Skylab.Shared.Domain.Entities;

namespace Skylab.Feedbacks.Domain.Entities;

public class Feedback : BaseEntity
{
    public FeedbackTopic Topic { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? SentBy { get; set; }
    public FeedbackSource Source { get; set; }
    public FeedbackSentTo? SentTo { get; set; }
}