using Skylab.Shared.Application.Contracts.Mail;

namespace Skylab.Shared.Application.Services;

public interface IMailDispatcher
{
    bool Enqueue(SingleMailRequest request);
}