

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Polling;
using Telegram.Bot;

namespace CinemaBot.ServiceLayer;

public class BotBackGroundService : BackgroundService
{
    private readonly ILogger<BotBackGroundService> _logger;
    private readonly TelegramBotClient _client;
    private readonly IUpdateHandler _handler;

    public BotBackGroundService(ILogger<BotBackGroundService> logger,TelegramBotClient client,IUpdateHandler handler)
    {
        _logger = logger;
        _client = client;
        _handler = handler;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bot = await _client.GetMeAsync(stoppingToken);
        _logger.LogInformation($"Bot started successfully {bot.Username}");
        _client.StartReceiving(_handler.HandleUpdateAsync,_handler.HandlePollingErrorAsync, new ReceiverOptions
        {
            ThrowPendingUpdates = true,

        },stoppingToken);
    }
}
