using Skylab.Forms.Domain.Entities;

namespace Skylab.Forms.Application.Abstractions.Storage;

// Bu PR kapsamında sadece okuma metotları var.
// Yazma metotları (Submit/UpdateStatus/Archive) FormResponseService refactor edildiğinde eklenecek.
public interface IFormResponseRepository
{
    Task<FormResponse?> GetLatestForUserAsync(Guid formId, Guid userId, CancellationToken ct = default);
    Task<FormResponseCounts> GetCountsAsync(Guid formId, CancellationToken ct = default);
}

public sealed record FormResponseCounts(int Total, int Waiting, double? AverageTimeSpentSeconds);
