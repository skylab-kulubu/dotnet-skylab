using Skylab.Shared.Application.Contracts.Mail;

namespace Skylab.Shared.Application.Services;

public interface ISkyMailService
{
    Task<bool> SendSingleAsync(SingleMailRequest request, CancellationToken ct = default);
}