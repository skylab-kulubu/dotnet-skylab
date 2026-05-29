using Skylab.Forms.Domain.Entities;

namespace Skylab.Forms.Application.Services;

public interface IFormMailNotifier
{
    Task NotifyResponseCopyAsync(Form form, FormResponse response, CancellationToken ct = default);
    Task NotifyStatusChangedAsync(Form form, FormResponse response, CancellationToken ct = default);
}