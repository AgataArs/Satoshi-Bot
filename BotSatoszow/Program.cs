using System;
using Telegram.Bot;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BotSatoszow
{
    class Program
    {
        // Bot po odpaleniu nasłuchuje nowych wiadomości do niego na grupie od wyznaczonych osób - OK.
        // Posiada bazę danych użytkowników ID / ilość warningów 
        // Na podstawie komendy dodaje nowy warning i/lub banuje uzytkownika na dany okres

        public static TelegramBotClient Client;
        private static long ChatId = -632365671; //Wpisać chat id grupy docelowej (tzw. produkcyjnej)
        private static List<long> AdminUserIds = new List<long>() { 1900853433 };
        private static JsonDatabase database = new JsonDatabase();
        private static User MeUser;
        private const int WarningsForBans = 4;

        static async Task Main(string[] args)
        {
            Client = new TelegramBotClient("5288355324:AAFqjC2jalR_pQEOD77sz90ZdO5cDQr2QXU");
            var chat = await Client.GetChatAsync(ChatId);
            
          
            using var cts = new CancellationTokenSource();

            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { } // receive all update types
            };
            Client.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token);

            MeUser = await Client.GetMeAsync();

            Console.WriteLine($"Start listening for @{MeUser.Username}");
            Console.ReadLine();

            // Send cancellation request to stop bot

        }

        static async Task HandleUpdateAsync(ITelegramBotClient Client, Update update, CancellationToken cancellationToken)
        {
            // Only process Message updates: https://core.telegram.org/bots/api#message
            if (update.Type != UpdateType.Message)
                return;
            // Only process text messages
            if (update.Message!.Type != MessageType.Text)
                return;

            var chatId = update.Message.Chat.Id;
            var messageText = update.Message.Text.Trim().ToLower();

            if (chatId == ChatId && AdminUserIds.Contains(update.Message.From.Id))
            {
                //Komendy zdefiniowane i ich parsowanie 

                // example: /warn id


                var isWarnCommand = messageText.StartsWith("/warn");

                
                if(isWarnCommand)
                {
                    // 1900853433
                    var warnedUserId = Regex.Match(update.Message.Text, @"\d*$").Value;
                    ChatMember warnedUser = null;
                    var longWarnedUserId = long.Parse(warnedUserId);
                    try
                    {
                        warnedUser = await Client.GetChatMemberAsync(update.Message.Chat.Id, longWarnedUserId);
                    }
                    catch (Exception e)
                    {
                        // ignore - because the user does not exists.
                    }

                    //Czy taki uzytkownik jest na grupie
                    if (warnedUser == null) // nie ma
                    {
                        //Jesli nie to wyrzucic blad
                        Console.WriteLine("Error.There is no such member in the group");
                    }
                    else // jest
                    {
                        Console.WriteLine("There is such a member in the group");
                        //Jesli tak to dodac +1 do warningow w bazie danych dla tego uzytkownika
                        database.AddWarningToUser(longWarnedUserId);

                        await Client.DeleteMessageAsync(chatId, update.Message.MessageId);

                        //Jesli ma wiecej warningow niz x to banuj

                        //await Client.UnbanChatMemberAsync(chatId, longWarnedUserId);
                        if (database.UserDataDictionary[longWarnedUserId].WarningsCount >= WarningsForBans)
                        {
                            await Client.BanChatMemberAsync(chatId, longWarnedUserId, DateTime.Now.AddYears(3));
                            await Client.SendTextMessageAsync(chatId, $"User {warnedUser.User.FirstName} {warnedUser.User.LastName} (${warnedUser.User?.Username}) has been banned. ");
                        }
                        else
                        {
                            await Client.SendTextMessageAsync(chatId, $"User {warnedUser.User.FirstName} {warnedUser.User.LastName} (${warnedUser.User?.Username}) has been warned. " +
                                $"{WarningsForBans - database.UserDataDictionary[longWarnedUserId].WarningsCount} for a ban.");
                        }

                    }
                }
            }
        }

        static Task HandleErrorAsync(ITelegramBotClient Client, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

    }
}
