
using CinemaBot.DataLayer;
using CinemaBot.DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using User = CinemaBot.DataLayer.Entities.User;

namespace CinemaBot.ServiceLayer;

public partial class BotUpdateHandler : IUpdateHandler
{
    private readonly ILogger<BotUpdateHandler> _logger;
    //private readonly TelegramBotContext _context;
    private readonly IServiceProvider _serviceProvider;
    public BotUpdateHandler(ILogger<BotUpdateHandler> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Update type Unknown {exception.Message}", exception.Message);
        return Task.CompletedTask;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<TelegramBotContext>();

        var handler = update.Type switch
        {
            UpdateType.Message => HandleMessageAsync(botClient, update, cancellationToken),
            UpdateType.CallbackQuery => HandleCallbackQueryAsync(botClient, update, cancellationToken),
            //UpdateType.EditedMessage => HandleEditedMessageAsync(botClient, update.EditedMessage, cancellationToken),
            _ => HandleUnknownUpdateAsync(botClient, update, cancellationToken),

        };

        try
        {
            await handler;
        }
        catch (Exception ex)
        {
            await HandlePollingErrorAsync(botClient, ex, cancellationToken);
        }
    }
    private async Task HandleMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(nameof(update.Message));
        var handler = update.Message?.Text switch
        {
            "/start" => StartHandlerMessageAsync(botClient, update, cancellationToken),
            "/menu" => HandleAdminMenuAsync(botClient, update, cancellationToken),
            "/my_profile_id" => HandleMyProfileIdCommandAsync(botClient, update, cancellationToken),
            "➕ Admin qo‘shish" => HandleAdminAdditionAsync(botClient, update, cancellationToken),
            "📊 Statistikalar" => HandleAllUserStatAsync(botClient, update, cancellationToken),
            "📢 E’lon berish" => HandleAllUserMessageAsync(botClient, update, cancellationToken),
            "➕ Kanal qo‘shish" => HandleAddChannelAsync(botClient, update, cancellationToken),
            "➕ Film qo‘shish" => HandleFilmAdditionAsync(botClient, update, cancellationToken),
            //UpdateType.EditedMessage => HandleEditedMessageAsync(botClient, update.EditedMessage, cancellationToken),
            _ => HandleUnknownMessageUpdateAsync(botClient, update, cancellationToken),
        };


        try
        {
            await handler;
        }
        catch (Exception ex)
        {
            await HandlePollingErrorAsync(botClient, ex, cancellationToken);
        }
        var from = update.Message?.From;

        _logger.LogInformation($"Received message from {from?.FirstName}");
    }

    private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(nameof(update.Message));
        var handler = update.CallbackQuery.Data switch
        {
            "verify_subscription" => VerifySubscriptionAsync(botClient, update, cancellationToken),
            _ => HandleUnknownMessageUpdateAsync(botClient, update, cancellationToken),
        };

        try
        {
            await handler;
        }
        catch (Exception ex)
        {
            await HandlePollingErrorAsync(botClient, ex, cancellationToken);
        }
        var from = update.Message?.From;

        _logger.LogInformation($"Received message from {from?.FirstName}");
    }
    private Task HandleUnknownUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Update type Unknown {update.Type}", update.Type);

        return Task.CompletedTask;
    }

    private async Task HandleUnknownMessageUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<TelegramBotContext>();

        var user = await _context.Users.FirstOrDefaultAsync(s => s.ChatId == update.Message.From.Id);

        var userState = await _context.UserActionStates.FirstOrDefaultAsync(s => s.UserId == user.Id);

        var userFilm = await _context.UserFilms.FirstOrDefaultAsync(s => s.UserId == user.Id && s.AddedKey == false);
        if (user != null && userState.ForAddKey && userFilm != null)
        {
            string key = update.Message.Text.Trim(); // Foydalanuvchi kiritgan kalit

            var film = await _context.Films.FirstOrDefaultAsync(s => s.Id == userFilm.FilmId);

            film.Key = key;
            userFilm.AddedKey = true;

            await _context.SaveChangesAsync();

            await botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: $"✅ Xabar saqlandi! Kalit: `{key}`",
                parseMode: ParseMode.Markdown);
            userState.ForAddKey = false;
            await _context.SaveChangesAsync();
        }
        else if (user != null && userState.ForAddChannel)
        {
           await HandleChannelUsernameAsync(botClient,update,cancellationToken);
        }
        else if (user != null && userState.ForAddAdmin)
        {
            await HandleChatIdForAdminAsync(botClient, update, cancellationToken);
        }
        else if (update.Type == UpdateType.Message && update.Message != null)
        {
            var message = update.Message;
            long userChatId = message.From.Id;

            // 1️⃣ Agar xabar KANALDAN forward qilingan bo'lsa, kalit so'zni kutamiz
            if (message.ForwardFromChat != null && message.ForwardFromChat.Type == ChatType.Channel)
            {

                if (userState.ForAllUserMessage)
                {
                    var members = _context.Set<User>().Where(s => !s.IsAdmin).ToList();
                    var admins = _context.Set<User>().Where(s => s.IsAdmin).ToList();

                    foreach(var member in members)
                    {
                        await botClient.CopyMessageAsync(
                    chatId: member.ChatId, 
                    fromChatId: update.Message.ForwardFromChat.Id,  
                    messageId: update.Message.ForwardFromMessageId.Value   
                    );
                    }
                    foreach (var admin in admins)
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: admin.ChatId,
                        text: $"📌 Foydalanuvchilarga xabar yuborildi. ID: {update.Message.From.Id} - {user.FullName}",
                        parseMode: ParseMode.Markdown);
                    }

                    userState.ForAllUserMessage = false;
                    await _context.SaveChangesAsync();
                }
                else if (userState.ForAddFilm)
                {
                    long channelId = message.ForwardFromChat.Id;
                    int messageId = message.ForwardFromMessageId ?? 0;

                    if (messageId > 0 && userState.ForAddFilm)
                    {
                        var film = new Film()
                        {
                            IsActive = true,
                            MessageId = message.ForwardFromMessageId.Value,
                            TelegramChannelId = message.ForwardFromChat.Id,
                        };

                        _context.Set<Film>().Add(film);
                        await _context.SaveChangesAsync();

                        var addUserFilm = new UserFilm()
                        {
                            AddedKey = false,
                            UserId = user.Id,
                            ChatId = userChatId,
                            FilmId = film.Id,
                        };
                        _context.Set<UserFilm>().Add(addUserFilm);

                        userState.ForAddFilm = false;
                        userState.ForAddKey = true;

                        await _context.SaveChangesAsync();

                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "📌 Iltimos, ushbu xabar uchun kalit so‘z kiriting:",
                            parseMode: ParseMode.Markdown);
                    }
                }
                
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "❌ Forward qilingan xabar noto‘g‘ri!");
                }
            }
            else
            {
                await ForwardSavedMessage(botClient, update, cancellationToken);
            }


        }
    }

    private async Task HandleAdminMenuAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long userChatId = update.Message.From.Id;

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<TelegramBotContext>();

        // 1️⃣ Foydalanuvchining isAdmin ekanligini bazadan tekshiramiz
        var user = await _context.Users.FirstOrDefaultAsync(u => u.ChatId == userChatId);

        if (user != null && user.IsAdmin) // Agar admin bo'lsa
        {
            var replyMarkup = new ReplyKeyboardMarkup(new[]
            {
             new KeyboardButton[] { "📊 Statistikalar", "📢 E’lon berish" },
            new KeyboardButton[] { "➕ Film qo‘shish", "➕ Admin qo‘shish" },
            new KeyboardButton[] { "➕ Kanal qo‘shish", "🔙 Orqaga" }
        })
            {
                ResizeKeyboard = true
            };

            await botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: "📌 Admin menyusi:",
                replyMarkup: replyMarkup);
        }
        else
        {
            await botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: "❌ Sizda admin ruxsati yo‘q!");
        }
    }

    private async Task HandleAdminAdditionAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long userId = update.Message.From.Id;

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<TelegramBotContext>();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.ChatId == userId);
        var userState = await _context.UserActionStates.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (user != null && user.IsAdmin)
        {
            userState.ForAddAdmin = true;
            await _context.SaveChangesAsync(); // Foydalanuvchidan `ChatId` kutamiz

            await botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: "📌 Admin qilish uchun foydalanuvchining `Chat ID` sini yuboring:");
        }
    }

    private async Task HandleFilmAdditionAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long userId = update.Message.From.Id;

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<TelegramBotContext>();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.ChatId == userId);
        var userState = await _context.UserActionStates.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (user != null && user.IsAdmin)
        {
            userState.ForAddFilm = true;
            await _context.SaveChangesAsync(); // Foydalanuvchidan `ChatId` kutamiz

            await botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: "📌 Film qo'shish uchun kanaldan filmni forward qiling:");
        }
    }

    private async Task HandleAllUserMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long userId = update.Message.From.Id;

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<TelegramBotContext>();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.ChatId == userId);
        var userState = await _context.UserActionStates.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (user != null && user.IsAdmin)
        {
            userState.ForAllUserMessage = true;
            await _context.SaveChangesAsync(); // Foydalanuvchidan `ChatId` kutamiz

            await botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: "📌 Barcha foydalanuvchilarga xabar yuborish uchun kanaldan xabarni forward qiling:");
        }
    }

    private async Task HandleChatIdForAdminAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long adminId = update.Message.From.Id;
        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<TelegramBotContext>();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.ChatId == adminId);
        var userState = await _context.UserActionStates.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (user != null && userState.ForAddAdmin)
        {
            
            if (!long.TryParse(update.Message.Text, out long targetChatId))
            {
                await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: "❌ Noto‘g‘ri Chat ID! Iltimos, to‘g‘ri raqam kiriting.");
                return;
            }

            // 🔍 Chat ID bo‘yicha foydalanuvchini bazadan qidiramiz
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.ChatId == targetChatId);

            if (targetUser != null)
            {
                targetUser.IsAdmin = true;
                
                await _context.SaveChangesAsync();

                await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: $"✅ {targetChatId} endi admin!");

                // 🎯 Admin qilingan foydalanuvchiga ham xabar yuboramiz
                await botClient.SendTextMessageAsync(
                    chatId: targetChatId,
                    text: "✅ Sizga admin ruxsatlari berildi! /menu ni bosing.");

                userState.ForAddAdmin = false;
                await _context.SaveChangesAsync();

            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: "❌ Bunday `Chat ID` li foydalanuvchi botdan foydalanmagan.");
            }
        }
    }
    private async Task StartHandlerMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var userChatId = update.Message?.From?.Id;
        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var scope = scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<TelegramBotContext>();
        var user = await _context.Set<User>().FirstOrDefaultAsync(s => s.ChatId == userChatId);

        var requiredChannels = await _context.Set<Channel>().Where(s => s.IsActive).ToListAsync();
        if (user == null)
        {
            var added = new User();
            added.ChatId = userChatId.Value;
            added.FullName = update.Message.From.FirstName;
            added.IsAdmin = false;

            _context.Set<User>().Add(added);
            await _context.SaveChangesAsync();

            var addUserState = new UserActionState()
            {
                ChatId = userChatId.Value,
                UserId = added.Id,
                ForAddChannel = false,
                ForAddAdmin = false,
                ForAddFilm = false,
                ForAddKey = false,
                ForAllUserMessage = false,
            };

            _context.Set<UserActionState>().Add(addUserState);
            await _context.SaveChangesAsync();
        }

        user = await _context.Set<User>().FirstOrDefaultAsync(s => s.ChatId == userChatId);

        List<string> notJoinedChannels = new List<string>();

        foreach (var channel in requiredChannels)
        {
            bool isMember = await IsUserInChannel(botClient, userChatId.Value, $"@{channel.Link}");
            if (!isMember)
            {
                notJoinedChannels.Add(channel.Link);
            }
        }
        if (notJoinedChannels.Any()) // Agar foydalanuvchi barcha kanallarga obuna bo‘lmagan bo‘lsa
        {
            string joinMessage = "❌ Kechirasiz, botimizdan foydalanishdan oldin ushbu kanallarga a'zo bo'lishingiz kerak:\n\n";

            var inlineKeyboard = new List<List<InlineKeyboardButton>>();
            foreach (var channel in notJoinedChannels)
            {
                inlineKeyboard.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithUrl($"{channel.Replace("@", "")}", $"https://t.me/{channel}")
                });
            }

            // Tasdiqlash tugmasi
            inlineKeyboard.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("✅ Tasdiqlash", "verify_subscription")
            });

            await botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: joinMessage,
                parseMode: ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(inlineKeyboard)
            );
        }
        else
        {
            await botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: "✅ Xush kelibsiz! Siz barcha talab qilingan kanallarga a'zo bo'lgansiz."
            );
        }

    }
    private async Task ForwardSavedMessage(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var scope = scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<TelegramBotContext>();
        var films = await _context.Set<Film>().Where(s => s.IsActive).ToListAsync();
        string key = update.Message.Text.Trim();
        if (films.Any(s => s.Key == key))
        {
            films = films.Where(s => s.Key == key).ToList();

            try
            {
                foreach (var film in films)
                {
                    long channelId = film.TelegramChannelId;
                    int messageId = film.MessageId;
                     await botClient.CopyMessageAsync(
                     chatId: update.Message.Chat.Id, // Xabarni forward qilish kerak bo‘lgan chat
                     fromChatId: channelId,  // Asl kanal ID'si
                     messageId: messageId    // Forward qilinayotgan xabar ID'si
                     );
               }
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(update.Message.Chat.Id, $"❌ Xabarni forward qilishda xatolik: {ex.Message}");
            }
        }
        else
        {
            await botClient.SendTextMessageAsync(update.Message.Chat.Id, "❌ Bu kalit bo‘yicha xabar topilmadi!");
        }
    }
    static async Task<bool> IsUserInChannel(ITelegramBotClient botClient, long userId, string channelUsername)
    {
        try
        {
            var chatMember = await botClient.GetChatMemberAsync(channelUsername, userId);
            return chatMember.Status == ChatMemberStatus.Member || chatMember.Status == ChatMemberStatus.Administrator || chatMember.Status == ChatMemberStatus.Creator;
        }
        catch
        {
            return false;
        }
    }
    private async Task HandleMyProfileIdCommandAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = update.Message.Chat.Id;

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"📌 Sizning Chat ID: `{chatId}`",
            parseMode: ParseMode.Markdown);
    }

    private async Task HandleAddChannelAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long userId = update.Message.From.Id;

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<TelegramBotContext>();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.ChatId == userId);

        var userState = await _context.UserActionStates.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (user != null && user.IsAdmin)
        {
            userState.ForAddChannel = true;
            await _context.SaveChangesAsync();// Foydalanuvchidan kanal username kutamiz

            await botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: "📌 Kanal qo‘shish uchun kanalning `username` ni yuboring: @user_name ");
        }
    }

    private async Task HandleAllUserStatAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long userId = update.Message.From.Id;

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<TelegramBotContext>();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.ChatId == userId);
        var userCount =  _context.Users.Count();
        if (user != null && user.IsAdmin)
        {

            await botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: $"📊 Botdan foydalanayotganlar soni: {userCount}");
        }
    }
    private async Task HandleChannelUsernameAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long userId = update.Message.From.Id;
        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<TelegramBotContext>();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.ChatId == userId);

        var userState = await _context.UserActionStates.FirstOrDefaultAsync(s => s.UserId == user.Id);

        if (userState.ForAddChannel)
        {
            string username = update.Message.Text?.Trim();
            
          
            try
            {
                Chat chat = await botClient.GetChatAsync($"@{username}");
                long channelId = chat.Id;

                var newChannel = new Channel()
                {
                    ChatId = channelId,
                    Link = username,
                    IsActive = true
                };

                _context.Set<Channel>().Add(newChannel);
                await _context.SaveChangesAsync();

                await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: $"✅ `{username}` kanali qo‘shildi! `Chat ID: {channelId}`",
                    parseMode: ParseMode.Markdown);

                userState.ForAddChannel = false;
                await _context.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: "❌ Xatolik yuz berdi! Ehtimol, bot ushbu kanalga admin qo‘shilmagan yoki username noto‘g‘ri.");
            }
        }
    }

    private async Task VerifySubscriptionAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long userChatId = update.CallbackQuery.From.Id;

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<TelegramBotContext>();

        var requiredChannels = await _context.Set<Channel>().Where(s => s.IsActive).ToListAsync();
        List<string> notJoinedChannels = new List<string>();

        foreach (var channel in requiredChannels)
        {
            bool isMember = await IsUserInChannel(botClient, userChatId, $"@{channel.Link}");
            if (!isMember)
            {
                notJoinedChannels.Add(channel.Link);
            }
        }

        if (notJoinedChannels.Any()) // Agar hamon obuna bo'lmagan kanallar bo'lsa
        {
            string joinMessage = "❌ Siz hali quyidagi kanallarga a’zo bo‘lmagansiz:\n\n";

            var inlineKeyboard = new List<List<InlineKeyboardButton>>();
            foreach (var channel in notJoinedChannels)
            {
                inlineKeyboard.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithUrl($"{channel.Replace("@", "")}", $"https://t.me/{channel}")
            });
            }

            // Tasdiqlash tugmasini qayta qo‘shamiz
            inlineKeyboard.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("✅ Tasdiqlash", "verify_subscription")
        });

            await botClient.EditMessageTextAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                messageId: update.CallbackQuery.Message.MessageId,
                text: joinMessage,
                parseMode: ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(inlineKeyboard)
            );
        }
        else
        {
            await botClient.EditMessageTextAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                messageId: update.CallbackQuery.Message.MessageId,
                text: "✅ Botdan to‘liq foydalanishingiz mumkin!"
            );
        }
    }
   

}
