using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
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

        private static Dictionary<int, ConversationState> ConversationStates { get; set; } = new Dictionary<int, ConversationState>();

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
                Bot = new TelegramBotClient(File.ReadAllText(keyFile));
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
            try
            {
                Username = Bot.GetMeAsync().Result.Username;

                var result = Bot.GetUpdatesAsync(-1, 1).Result;
                if (result.Length > 0) Bot.GetUpdatesAsync(result[0].Id + 1).Wait();

                Bot.OnMessage += BotOnMessageReceived;
                Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
                Bot.OnInlineQuery += BotOnInlineQueryReceived;
                Bot.OnReceiveError += BotOnReceiveError;

                Bot.StartReceiving();
                Console.WriteLine($"Server started listening, Bot ready");

            commandLoop:
                string command = Console.ReadLine();
                if (command == "/stop")
                {
                    Console.WriteLine("Exiting");
                    Environment.Exit(0);
                }
                else
                {
                    goto commandLoop;
                }

                Bot.StopReceiving();
            }
            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
        }

        #region userInteraction
        public static void WelcomeUser(int UserId)
        {
            Send(UserId, "Welcome to Systemical Consensus Bot, the bot that finally decides: Where do we wanna eat?\nTo proceed, please send me the topic for your poll.");

            if (ConversationStates.ContainsKey(UserId))
            {
                ConversationStates[UserId].InteractionState = ConversationState.InteractionStates.TopicAsked;
            }

            else
            {
                ConversationStates.Add(UserId, new ConversationState());
            }
        }

        public static void RemoveUser(int UserId)
        {
            if (ConversationStates.ContainsKey(UserId)) ConversationStates.Remove(UserId);
        }
        #endregion

        #region helpers
        public static void Send(long chatId, string message, IReplyMarkup markup = null)
        {
            Bot.SendTextMessageAsync(chatId, message, parseMode: ParseMode.Html, replyMarkup: markup);
        }
        #endregion

        private static void ClosePoll(CallbackQueryEventArgs e, Poll poll)
        {
            poll.Lock();

            databaseConnection.SavePoll(poll);


            Bot.EditMessageReplyMarkupAsync(e.CallbackQuery.InlineMessageId);

            Bot.EditMessageTextAsync(inlineMessageId: e.CallbackQuery.InlineMessageId, text: poll.GetPollMessage(), parseMode: ParseMode.Html);
        }

        #region BotEventHandlers
        private static void BotOnMessageReceived(object sender, MessageEventArgs e)
        {
            try
            {
                if (e.Message.Chat.Type != ChatType.Private) return;
                int UserId = e.Message.From.Id;

                if (e.Message.Entities?.Length > 0 && e.Message.Entities[0].Type == MessageEntityType.BotCommand)
                {
                    if (e.Message.Text == "/start help")
                    {
                        Send(UserId, $"{Username} by Olfi01 and Jupp56, version <i>{Version}</i>");
                        Send(UserId, Help);
                        return;
                    }

                    if (e.Message.Text == "/start new")
                    {
                        WelcomeUser(UserId);
                        return;
                    }

                    else if (e.Message.Text == "/about")
                    {
                        Send(UserId, $"{Username} by Olfi01 and Jupp56, version <i>{Version}</i>");
                        Send(UserId, About);
                        return;
                    }

                    else if (e.Message.Text == "/delete")
                    {
                        InlineKeyboardMarkup markup = GetDeleteMarkup(UserId);

                        Bot.SendTextMessageAsync(UserId, "Choose one or several polls to delete", replyMarkup: markup);
                        return;
                    }

                    else if (e.Message.Text == "/version")
                    {
                        Send(UserId, $"{Username} by Olfi01 and Jupp56, version <i>{Version}</i>");
                        return;
                    }

                    if (!ConversationStates.ContainsKey(UserId))
                    {
                        WelcomeUser(UserId);
                        return;
                    }
                    switch (e.Message.EntityValues.ElementAt(0))
                    {
                        case "/cancel":
                        case "/stop":
                            RemoveUser(UserId);
                            Send(UserId, "Canceled whatever I was doing just now.");
                            return;
                    }
                }

                if (ConversationStates.ContainsKey(UserId))
                {
                    var state = ConversationStates[UserId];
                    switch (state.InteractionState)
                    {
                        case ConversationState.InteractionStates.TopicAsked:
                            state.Topic = e.Message.Text.Escape();
                            Send(UserId, $"Set topic to \"{state.Topic}\"! Send me your first answer now.");
                            state.InteractionState = ConversationState.InteractionStates.AnswerAsked;
                            break;
                        case ConversationState.InteractionStates.AnswerAsked:
                            if (e.Message.Text == "/done" || e.Message.Text == "/save")
                            {
                                if (state.Answers.Count >= 2)
                                {
                                    databaseConnection.SavePoll(new Poll(state.Topic, UserId, state.Answers.ToArray()));
                                    Send(UserId, "Poll was saved successfully!", new InlineKeyboardMarkup(new InlineKeyboardButton { SwitchInlineQuery = state.Topic, Text = "Share poll" }));
                                    RemoveUser(UserId);
                                }
                                else
                                {
                                    Send(UserId, "Not enough answers provided, please provide at least two.");
                                }
                            }
                            else
                            {
                                state.Answers.Add(e.Message.Text.Escape());
                                Send(UserId, $"Added answer \"{e.Message.Text.Escape()}\". Send me another answer{(state.Answers.Count > 1 ? " or send /done if you're finished." : ".")}");
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Send(devChatId, ex.ToString());
            }
        }

        private static InlineKeyboardMarkup GetDeleteMarkup(int UserId)
        {
            List<Poll> pollsOfUser = databaseConnection.GetPollsByOwner(UserId);

            List<InlineKeyboardButton[]> rows = new List<InlineKeyboardButton[]>();

            foreach (Poll poll in pollsOfUser)
            {
                InlineKeyboardButton[] row = new InlineKeyboardButton[1];
                row[0] = new InlineKeyboardButton() { CallbackData = $"delete:{poll.PollId}", Text = $"{poll.Topic}" };
                rows.Add(row);
            }

            rows.Add(new InlineKeyboardButton[1] { new InlineKeyboardButton() { CallbackData = $"doneDelete", Text = $"Done" } });
            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(rows);
            return markup;
        }

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs e)
        {
            Send(devChatId, e.ApiRequestException.ToString());
        }

        private static void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs e)
        {
            var userId = e.InlineQuery.From.Id;
            var polls = databaseConnection.GetPollsByOwner(userId);
            List<InlineQueryResultBase> results = new List<InlineQueryResultBase>();
            polls = polls.OrderBy(x => ModifiedLevenshteinDistance(x.Topic, e.InlineQuery.Query)).Take(Math.Min(polls.Count, 50)).ToList();
            foreach (var poll in polls)
            {
                var content = new InputTextMessageContent(poll.GetPollMessage()) { ParseMode = ParseMode.Html };
                var result = new InlineQueryResultArticle($"sendpoll:{poll.PollId}", poll.Topic.Unescape(), content) { ReplyMarkup = poll.GetInlineKeyboardMarkup() };
                results.Add(result);
            }
            Bot.AnswerInlineQueryAsync(e.InlineQuery.Id, results, isPersonal: true, cacheTime: 0, switchPmText: "Create new poll", switchPmParameter: "new");
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
                var temp = target;
                target = source;
                source = temp;
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

        private static void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs e)
        {
            try
            {
                int userId = e.CallbackQuery.From.Id;
                string queryId = e.CallbackQuery.Id;
                string[] data = e.CallbackQuery.Data.Split(':');

                if (!(data is null) && data[0] != null)
                {
                    switch (data[0])
                    {
                        case "null":
                            Bot.AnswerCallbackQueryAsync(queryId);
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
                                    Bot.AnswerCallbackQueryAsync(queryId, text: $"Your vote for option {answerIndex.ToString()} was changed to: {newValue}", showAlert: false);
                                    if (!hasVoted) Bot.EditMessageTextAsync(inlineMessageId: e.CallbackQuery.InlineMessageId, text: poll.GetPollMessage(), replyMarkup: poll.GetInlineKeyboardMarkup(), parseMode: ParseMode.Html);
                                }
                                else
                                {
                                    Bot.AnswerCallbackQueryAsync(queryId, "Vote could not be changed. This Poll is not active anymore.");
                                    ClosePoll(e, poll);
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

                                Bot.AnswerCallbackQueryAsync(queryId, text: results, showAlert: true);
                            }
                            break;

                        case "showone":
                            {
                                long pollId = Convert.ToInt64(data[1]);
                                Poll poll = databaseConnection.GetPoll(pollId);
                                string result = $"Current resistance value for option {data[2]}: {poll.GetUserVotes(userId)[Convert.ToInt32(data[2])]}";
                                Bot.AnswerCallbackQueryAsync(queryId, result);
                                break;
                            }

                        case "close":
                            {
                                long pollId = Convert.ToInt64(data[1]);
                                Poll poll = databaseConnection.GetPoll(pollId);
                                if (userId == poll.OwnerId)
                                {
                                    ClosePoll(e, poll);
                                    Bot.AnswerCallbackQueryAsync(queryId);
                                }
                                else
                                {
                                    string result = "You cannot close this poll, as you did not create it";
                                    Bot.AnswerCallbackQueryAsync(queryId, result);
                                }


                                break;
                            }

                        case "delete":
                            {
                                long pollId = Convert.ToInt64(data[1]);
                                databaseConnection.DeletePoll(pollId);
                                Bot.EditMessageTextAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId, text: "Items to delete", replyMarkup: GetDeleteMarkup(e.CallbackQuery.From.Id));
                                Bot.AnswerCallbackQueryAsync(queryId, text: "Poll deleted", showAlert: false);
                            }
                            break;
                        case "doneDelete":
                            {
                                Bot.AnswerCallbackQueryAsync(queryId);
                                Bot.DeleteMessageAsync(chatId: e.CallbackQuery.Message.Chat.Id, messageId: e.CallbackQuery.Message.MessageId);
                            }
                            break;


                    }
                }

            }
            catch (Exception ex)
            {
                Send(devChatId, ex.ToString());
            }
        }

        #endregion
    }
}
