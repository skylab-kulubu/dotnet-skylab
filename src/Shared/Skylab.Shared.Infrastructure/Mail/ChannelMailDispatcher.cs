using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Skylab.Shared.Application.Contracts.Mail;
using Skylab.Shared.Application.Services;

namespace Skylab.Shared.Infrastructure.Mail;

public class ChannelMailDispatcher : IMailDispatcher
{
    private const int Capacity = 1000;

    private readonly Channel<SingleMailRequest> _channel;
    private readonly ILogger<ChannelMailDispatcher> _logger;

    public ChannelMailDispatcher(ILogger<ChannelMailDispatcher> logger)
    {
        _logger = logger;
        _channel = Channel.CreateBounded<SingleMailRequest>(new BoundedChannelOptions(Capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropWrite
        });
    }

    public ChannelReader<SingleMailRequest> Reader => _channel.Reader;

    public bool Enqueue(SingleMailRequest request)
    {
        if (_channel.Writer.TryWrite(request)) return true;

        _logger.LogError("Mail kuyruğu dolu, istek düşürüldü (template {TemplateId}, alıcı {Recipient})", request.TemplateId, request.RecipientEmail);
        return false;
    }
}