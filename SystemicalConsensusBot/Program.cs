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
        private static TelegramBotClient Bot;
        private const long devChatId = -1001070844778;

        private static readonly DatabaseConnection databaseConnection = new DatabaseConnection(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SystemicalConsensusBot", "database.json"));

        private static Dictionary<int, ConversationState> ConversationStates { get; set; } = new Dictionary<int, ConversationState>();
        static void Main()
        {
            Console.WriteLine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SystemicalConsensusBot"));
            Bot = new TelegramBotClient(File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SystemicalConsensusBot", "key.txt")));

            var result = Bot.GetUpdatesAsync(-1, 1).Result;
            if (result.Length > 0) Bot.GetUpdatesAsync(result[0].Id + 1).Wait();

            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            Bot.OnInlineQuery += BotOnInlineQueryReceived;
            Bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
            //Bot.OnReceiveError += BotOnReceiveError;

            Bot.StartReceiving(Array.Empty<UpdateType>());
            Console.WriteLine($"Start listening");
            Console.ReadLine();
            Bot.StopReceiving();
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

        public static void Send(long chatId, string message, IReplyMarkup markup = null)
        {
            Bot.SendTextMessageAsync(chatId, message, parseMode: ParseMode.Html, replyMarkup: markup);
        }

        private static void ClosePoll(CallbackQueryEventArgs e, Poll poll)
        {
            poll.Lock();

            databaseConnection.SavePoll(poll);
            Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id);

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
                                databaseConnection.SavePoll(new Poll(state.Topic, UserId, state.Answers.ToArray()));
                                Send(UserId, "Poll was saved successfully!", new InlineKeyboardMarkup(new InlineKeyboardButton { SwitchInlineQuery = state.Topic, Text = "Share poll" }));
                                RemoveUser(UserId);
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

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs e)
        {
            Send(devChatId, e.ApiRequestException.ToString());
        }

        private static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private static void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs e)
        {
            var userId = e.InlineQuery.From.Id;
            var polls = databaseConnection.GetPollsByOwner(userId);
            List<InlineQueryResultBase> results = new List<InlineQueryResultBase>();
            if (polls.Count < 1)
            {
                Bot.AnswerInlineQueryAsync(e.InlineQuery.Id, results, isPersonal: true);
                return;
            }
            polls = polls.OrderBy(x => ModifiedLevenshteinDistance(x.Topic, e.InlineQuery.Query)).Take(Math.Min(polls.Count, 50)).ToList();
            foreach (var poll in polls)
            {
                var content = new InputTextMessageContent(poll.GetPollMessage()) { ParseMode = ParseMode.Html };
                var result = new InlineQueryResultArticle($"sendpoll:{poll.PollID}", poll.Topic.Unescape(), content) { ReplyMarkup = poll.GetInlineKeyboardMarkup() };
                results.Add(result);
            }
            Bot.AnswerInlineQueryAsync(e.InlineQuery.Id, results, isPersonal: true);
        }

        private static int ModifiedLevenshteinDistance(string source, string target)
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
            var distance = new int[2, m + 1];
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
                                distance[previousRow, j] + 10,
                                distance[currentRow, j - 1] + 10),
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
                string[] data = e.CallbackQuery.Data.Split(':');

                if (!(data is null) && data[0] != null)
                {
                    switch (data[0])
                    {
                        case "null":
                            Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id);
                            break;

                        case "vote":
                            {
                                long pollId = Convert.ToInt64(data[1]);
                                int answerIndex = Convert.ToInt32(data[2]);

                                int changeValueBy = data[3] == "+" ? 1 : -1;

                                Poll poll = databaseConnection.GetPoll(pollId);

                                bool result = poll.Vote(userId, answerIndex, changeValueBy, out int newValue);
                                if (result)
                                {
                                    databaseConnection.SavePoll(poll);
                                    Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, text: $"Your vote was changed to: {newValue}", showAlert: false);
                                }
                                else
                                {
                                    Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Vote could not be changed. This Poll is not active anymore.");
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

                                Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, text: results, showAlert: true);
                            }
                            break;
                        case "close":
                     
                            {

                                long pollId = Convert.ToInt64(data[1]);
                                Poll poll = databaseConnection.GetPoll(pollId);
                                if (userId == poll.OwnerId)
                                {
                                    ClosePoll(e, poll);
                                }
                                break;
                            }


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
