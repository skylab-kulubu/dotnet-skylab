using Skylab.Forms.Application.Contracts.Mail;

namespace Skylab.Forms.Application.Abstractions;

public interface ISkyMailService
{
    Task<bool> SendSingleAsync(SingleMailRequest request, CancellationToken ct = default);
}
