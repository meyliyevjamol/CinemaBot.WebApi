using CinemaBot.DataLayer;
using CinemaBot.ServiceLayer;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;

var builder = WebApplication.CreateBuilder(args);
//AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);


builder.Services.AddDbContext<CinemaBot.DataLayer.TelegramBotContext>(options =>
    options.UseNpgsql("Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=telegram_bot;"));

var token = builder.Configuration.GetValue("BotToken", string.Empty);
builder.Services.AddSingleton(p => new TelegramBotClient(token));
builder.Services.AddSingleton<IUpdateHandler, BotUpdateHandler>();
builder.Services.AddHostedService<BotBackGroundService>();

var app = builder.Build();

app.Run();
