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
            Send(UserId, "Welcome to Systemical Consensus Bot, the bot that finally decides: Where do we wanna eat?\nTo proceed, please send me the topic for you poll.");

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

        public static void Send(int userId, string message)
        {
            Bot.SendTextMessageAsync(userId, message);
        }

        #region BotEventHandlers
        private static void BotOnMessageReceived(object sender, MessageEventArgs e)
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
                        state.Topic = e.Message.Text;
                        Send(UserId, $"Set topic to \"{state.Topic}\"! Send me your first answer now.");
                        state.InteractionState = ConversationState.InteractionStates.AnswerAsked;
                        break;
                    case ConversationState.InteractionStates.AnswerAsked:
                        if (e.Message.Text == "/done" || e.Message.Text == "/save")
                        {
                            databaseConnection.SavePoll(new Poll(UserId, state.Answers.Count, state.Answers.ToArray()));
                            Send(UserId, "Poll was saved successfully!");   //TODO send and share poll, inline keyboard
                            RemoveUser(UserId);
                        }
                        else
                        {
                            state.Answers.Add(e.Message.Text);
                            Send(UserId, $"Added answer \"{e.Message.Text}\". Send me another answer{(state.Answers.Count > 1 ? " or send /done if you're finished." : ".")}");
                        }
                        break;
                }
            }
        }

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion


    }
}
