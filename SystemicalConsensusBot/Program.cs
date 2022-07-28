using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace SystemicalConsensusBot
{
    class Program
    {
        #region help and description strings, version
        private const string About = ("<b>About this Bot</b>\n" +
                            "\n" +
                            "This is the systemic consensing Bot.\n" +
                            "\n" +
                            "<i>What is systemic consensing?</i>\n" +
                            "Systemic consensing is a way to vote. However, instead of voting for one specific option, every participant assignes a personal resistance value ranging from 0 to 10 to every option. In the end, the option with the lowest resistance value wins.\n" +
                            "\n" +
                            "<i>Why should I let people vote with this system?</i>\n" +
                            "This system reduces stress, as it does not return the best option for only some people, but the most acceptable option for as many participants as possible. Fewer people are unhappy with the end result.\n" +
                            "\n");

        private static string Help =>
                            "<i>How does this Bot work?</i>\n" +
                            "\n" +
                            "Create a new poll\n" +
                            "Type /start and follow the instructions provided to create a new poll.\n" +
                            "\n" +
                            "Share the poll\n" +
                            $"After pressing \"Share\", you can choose a chat to post the poll into. In any chat, you also can type @{Username}. It then shows your polls. To post it, click on the desired poll.\n" +
                            $"\n" +
                            $"Vote\n" +
                            $"\n" +
                            $"Increment or decrement the value by pressing + or - on the desired option. Clicking on the option number shows the current value, \"Show all\" shows all of them.\n" +
                            $"The owner (and only he) can close the vote. Then the results are shown.\n" +
                            $"\nFor further information, send /about";

        private static readonly string Version = "v1.0.1-Enhancements";
        #endregion

        private static readonly TelegramBotClient Bot;
        private const long devChatId = -1001070844778;
        private static string Username;
        public static string HelpLink => $"https://telegram.me/{Username}?start=help";

        private static readonly string dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SystemicalConsensusBot");
        private static readonly string databaseFile = Path.Combine(dataFolder, "database.json");
        private static readonly string keyFile = Path.Combine(dataFolder, "key.txt");

        private static readonly DatabaseConnection databaseConnection;

        private static Dictionary<long, ConversationState> ConversationStates { get; set; } = new Dictionary<long, ConversationState>();




        static Program()
        {
        Reload:
            try
            {
                databaseConnection = new DatabaseConnection(databaseFile);
            }
            catch
            {
                Console.WriteLine($"\nCould not read database. Ensure that the database.json file is in \"{databaseFile}\" and readable.\n\nPress r to retry loading all files or any other key to exit.");
                if (Console.ReadKey().KeyChar == 'r') goto Reload;
                Environment.Exit(1);
            }
            try
            {
                Bot = new TelegramBotClient(System.IO.File.ReadAllText(keyFile));
            }
            catch
            {
                Console.WriteLine($"\nCould not read key file. It should be located at {keyFile}. Ensure it is there, formatted correctly (just the key in the first line) and this programs has the permissions to read it.\n\nPress r to retry loading all files or any other key to exit.");
                if (Console.ReadKey().KeyChar == 'r') goto Reload;
                Environment.Exit(1);
            }
        }

        static void Main()
        {
            using var cts = new CancellationTokenSource();

            Username = Bot.GetMeAsync().Result.Username;

            var result = Bot.GetUpdatesAsync(-1, 1).Result;
            if (result.Length > 0) Bot.GetUpdatesAsync(result[0].Id + 1).Wait();

            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
            };

            Bot.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
            );




            Console.WriteLine($"Server started listening, Bot ready");

            while (Console.ReadLine() != "/stop")
            {

            }

            Console.WriteLine("Exiting");
            Environment.Exit(0);



        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var message = update.Message;

            if (message != null)
            {
                await BotOnMessageReceived(message);
            }

            if (update.CallbackQuery != null)
            {
                await BotOnCallbackQueryReceived(update.CallbackQuery);
            }

            if (update.InlineQuery != null) await BotOnInlineQueryReceived(update.InlineQuery);


        }


        private static async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            await Send(devChatId, exception.ToString());
        }


        #region userInteraction
        public static async Task WelcomeUser(long UserId)
        {
            await Send(UserId, "Welcome to Systemical Consensus Bot, the bot that finally decides: Where do we wanna eat?\nTo proceed, please send me the topic for your poll.");

            if (ConversationStates.ContainsKey(UserId))
            {
                ConversationStates[UserId].InteractionState = ConversationState.InteractionStates.TopicAsked;
            }

            else
            {
                ConversationStates.Add(UserId, new ConversationState());
            }
        }

        public static void RemoveUser(long UserId)
        {
            if (ConversationStates.ContainsKey(UserId)) ConversationStates.Remove(UserId);
        }
        #endregion

        #region helpers
        public static async Task Send(long chatId, string message, IReplyMarkup markup = null)
        {
            await Bot.SendTextMessageAsync(chatId, message, parseMode: ParseMode.Html, replyMarkup: markup);
        }
        #endregion

        private static async void ClosePoll(string inlineMessageId, Poll poll)
        {
            poll.Lock();

            databaseConnection.SavePoll(poll);


            await Bot.EditMessageReplyMarkupAsync(inlineMessageId);

            await Bot.EditMessageTextAsync(inlineMessageId, text: poll.GetPollMessage(), parseMode: ParseMode.Html);
        }

        #region BotEventHandlers
        private static async Task BotOnMessageReceived(Message m)
        {
            try
            {
                if (m.Chat.Type != ChatType.Private) return;
                long UserId = m.From.Id;
                string text = m.Text;
                if (m.Entities?.Length > 0 && m.Entities[0].Type == MessageEntityType.BotCommand)
                {
                    if (text == "/start help")
                    {
                        await Send(UserId, $"{Username} by Olfi01 and Jupp56, version <i>{Version}</i>");
                        await Send(UserId, Help);
                        return;
                    }
                    else if (text == "/start new")
                    {
                        await WelcomeUser(UserId);
                        return;
                    }

                    else if (text == "/about")
                    {
                        await Send(UserId, $"{Username} by Olfi01 and Jupp56, version <i>{Version}</i>");
                        await Send(UserId, About);
                        return;
                    }

                    else if (text == "/delete")
                    {
                        InlineKeyboardMarkup markup = GetDeleteMarkup(UserId);

                        await Bot.SendTextMessageAsync(UserId, "Choose one or several polls to delete", replyMarkup: markup);
                        return;
                    }

                    else if (text == "/version")
                    {
                        await Send(UserId, $"{Username} by Olfi01 and Jupp56, version <i>{Version}</i>");
                        return;
                    }

                    if (!ConversationStates.ContainsKey(UserId))
                    {
                        await WelcomeUser(UserId);
                        return;
                    }
                    switch (m.EntityValues.ElementAt(0))
                    {
                        case "/cancel":
                        case "/stop":
                            RemoveUser(UserId);
                            await Send(UserId, "Canceled whatever I was doing just now.");
                            return;
                    }
                }

                if (ConversationStates.ContainsKey(UserId))
                {
                    var state = ConversationStates[UserId];
                    string escapedText = text.Escape();
                    switch (state.InteractionState)
                    {
                        case ConversationState.InteractionStates.TopicAsked:
                            state.Topic = escapedText;
                            await Send(UserId, $"Set topic to \"{state.Topic}\"! Send me your first answer now.");
                            state.InteractionState = ConversationState.InteractionStates.AnswerAsked;
                            break;
                        case ConversationState.InteractionStates.AnswerAsked:
                            if (text == "/done" || text == "/save")
                            {
                                if (state.Answers.Count >= 2)
                                {
                                    databaseConnection.SavePoll(new Poll(state.Topic, UserId, state.Answers.ToArray()));
                                    await Send(UserId, "Poll was saved successfully!", new InlineKeyboardMarkup(new InlineKeyboardButton("Share poll") { SwitchInlineQuery = state.Topic }));
                                    RemoveUser(UserId);
                                }
                                else
                                {
                                    await Send(UserId, "Not enough answers provided, please provide at least two.");
                                }
                            }
                            else
                            {
                                state.Answers.Add(escapedText);
                                await Send(UserId, $"Added answer \"{escapedText}\". Send me another answer{(state.Answers.Count > 1 ? " or send /done if you're finished." : ".")}");
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                await Send(devChatId, ex.ToString());
            }
        }

        private static InlineKeyboardMarkup GetDeleteMarkup(long UserId)
        {
            List<Poll> pollsOfUser = databaseConnection.GetPollsByOwner(UserId);

            List<InlineKeyboardButton[]> rows = new();

            foreach (Poll poll in pollsOfUser)
            {
                InlineKeyboardButton[] row = new InlineKeyboardButton[1];
                row[0] = new InlineKeyboardButton(poll.Topic) { CallbackData = $"delete:{poll.PollId}" };
                rows.Add(row);
            }

            rows.Add(new InlineKeyboardButton[1] { new InlineKeyboardButton("Done") { CallbackData = $"doneDelete" } });
            InlineKeyboardMarkup markup = new(rows);
            return markup;
        }


        private static async Task BotOnInlineQueryReceived(InlineQuery query)
        {
            var userId = query.From.Id;
            var polls = databaseConnection.GetPollsByOwner(userId);
            List<InlineQueryResult> results = new();
            polls = polls.OrderBy(x => ModifiedLevenshteinDistance(x.Topic, query.Query)).Take(Math.Min(polls.Count, 50)).ToList();
            foreach (var poll in polls)
            {
                var content = new InputTextMessageContent(poll.GetPollMessage()) { ParseMode = ParseMode.Html };
                var result = new InlineQueryResultArticle($"sendpoll:{poll.PollId}", poll.Topic.Unescape(), content) { ReplyMarkup = poll.GetInlineKeyboardMarkup() };
                results.Add(result);
            }
            await Bot.AnswerInlineQueryAsync(query.Id, results, isPersonal: true, cacheTime: 0, switchPmText: "Create new poll", switchPmParameter: "new");
        }

        private static double ModifiedLevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source))
            {
                if (string.IsNullOrEmpty(target)) return 0;
                return target.Length;
            }
            if (string.IsNullOrEmpty(target)) return source.Length;

            if (source.Length > target.Length)
            {
                (source, target) = (target, source);
            }

            var m = target.Length;
            var n = source.Length;
            var distance = new double[2, m + 1];

            // Initialize the distance matrix
            for (var j = 1; j <= m; j++) distance[0, j] = j;

            var currentRow = 0;
            for (var i = 1; i <= n; ++i)
            {
                currentRow = i & 1;
                distance[currentRow, 0] = i;
                var previousRow = currentRow ^ 1;
                for (var j = 1; j <= m; j++)
                {
                    var cost = (target[j - 1] == source[i - 1] ? 0 : 1);
                    distance[currentRow, j] = Math.Min(Math.Min(
                                distance[previousRow, j] + 0.1,
                                distance[currentRow, j - 1] + 0.1),
                                distance[previousRow, j - 1] + cost);
                }
            }
            return distance[currentRow, m];
        }

        private static async Task BotOnCallbackQueryReceived(CallbackQuery query)
        {
            try
            {
                long userId = query.From.Id;
                string queryId = query.Id;
                string[] data = query.Data.Split(':');

                if (data is not null && data[0] != null)
                {
                    switch (data[0])
                    {
                        case "null":
                            await Bot.AnswerCallbackQueryAsync(queryId);
                            break;


                        case "vote":
                            {
                                long pollId = Convert.ToInt64(data[1]);
                                int answerIndex = Convert.ToInt32(data[2]);

                                int changeValueBy = data[3] == "+" ? 1 : -1;

                                Poll poll = databaseConnection.GetPoll(pollId);
                                bool hasVoted = poll.HasVoted(userId);


                                bool result = poll.Vote(userId, answerIndex, changeValueBy, out int newValue);
                                if (result)
                                {
                                    databaseConnection.SavePoll(poll);
                                    await Bot.AnswerCallbackQueryAsync(queryId, text: $"Your vote for option {answerIndex} was changed to: {newValue}", showAlert: false);
                                    if (!hasVoted) await Bot.EditMessageTextAsync(inlineMessageId: query.InlineMessageId, text: poll.GetPollMessage(), replyMarkup: poll.GetInlineKeyboardMarkup(), parseMode: ParseMode.Html);
                                }
                                else
                                {
                                    await Bot.AnswerCallbackQueryAsync(queryId, "Vote could not be changed. This Poll is not active anymore.");
                                    ClosePoll(query.InlineMessageId, poll);
                                }
                                break;
                            }

                        case "show":
                            {
                                string results = "Your current votes: \n";
                                long pollId = Convert.ToInt64(data[1]);
                                Poll poll = databaseConnection.GetPoll(pollId);
                                string[] answers = poll.Answers;
                                int[] userChoices = poll.GetUserVotes(userId);
                                for (int i = 0; i < poll.Answers.Length; i++)
                                {
                                    results += $"\n{i}. {answers[i]}: {userChoices[i]}";
                                }

                                await Bot.AnswerCallbackQueryAsync(queryId, text: results, showAlert: true);
                            }
                            break;

                        case "showone":
                            {
                                long pollId = Convert.ToInt64(data[1]);
                                Poll poll = databaseConnection.GetPoll(pollId);
                                string result = $"Current resistance value for option {data[2]}: {poll.GetUserVotes(userId)[Convert.ToInt32(data[2])]}";
                                await Bot.AnswerCallbackQueryAsync(queryId, result);
                                break;
                            }

                        case "close":
                            {
                                long pollId = Convert.ToInt64(data[1]);
                                Poll poll = databaseConnection.GetPoll(pollId);
                                if (userId == poll.OwnerId)
                                {
                                    ClosePoll(query.InlineMessageId, poll);
                                    await Bot.AnswerCallbackQueryAsync(queryId);
                                }
                                else
                                {
                                    string result = "You cannot close this poll, as you did not create it";
                                    await Bot.AnswerCallbackQueryAsync(queryId, result);
                                }

                                break;
                            }

                        case "delete":
                            {
                                long pollId = Convert.ToInt64(data[1]);
                                databaseConnection.DeletePoll(pollId);
                                await Bot.EditMessageTextAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId, text: "Items to delete", replyMarkup: GetDeleteMarkup(query.From.Id));
                                await Bot.AnswerCallbackQueryAsync(queryId, text: "Poll deleted", showAlert: false);
                            }
                            break;
                        case "doneDelete":
                            {
                                await Bot.AnswerCallbackQueryAsync(queryId);
                                await Bot.DeleteMessageAsync(chatId: query.Message.Chat.Id, messageId: query.Message.MessageId);
                            }
                            break;


                    }
                }

            }
            catch (Exception ex)
            {
                await Send(devChatId, ex.ToString());
            }
        }

        #endregion
    }
}
