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
        
        private static DatabaseConnection databaseConnection = new DatabaseConnection(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SystemicalConsensusBot", "database.json"));

        private static List<ConversationState> ConversationStates { get; set; } = new List<ConversationState>();
        static void Main(string[] args)
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
            send(UserId, "Welcome to Systemical_Consensus Bot, the bot that finally decides: Where do we wanna eat?\nTo proceed, please at first choose the desired topic of your poll with \"/topic <topic>\".\n To add answers, send \"/answer <answer>\"\nTo save the poll, use \"/save\"");

            if (ConversationStates.Exists(x => x.UserId == UserId))
            {
                ConversationStates.Find(x => x.UserId == UserId).InteractionState = ConversationState.InteractionStates.started;
            }

            else
            {
                ConversationStates.Add(new ConversationState(UserId));
            }
        }


        public static void RemoveUser(int UserId)
        {
            ConversationStates.Remove(ConversationStates.Find(x => x.UserId == UserId));
        }
        #endregion

        public static void send(int userId, string message)
        {
            Bot.SendTextMessageAsync(userId, message);
        }


        #region BotEventHandlers
        private static void BotOnMessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Message.Chat.Type != ChatType.Private) return;

            if (e.Message.Entities[0].Type == MessageEntityType.BotCommand)
            {
                int UserId = e.Message.From.Id;
                if (!ConversationStates.Exists(x => x.UserId == e.Message.From.Id)) WelcomeUser(UserId);
                switch (e.Message.EntityValues.ElementAt(0))
                {
                    case "/start":

                        break;

                    case "/cancel":
                    case "/stop":
                        RemoveUser(UserId);
                        break;

                    case "/topic":
                        try
                        {
                            string topic = e.Message.Text.Split(' ')[1];
                            ConversationStates.Find(x => x.UserId == UserId).Topic = topic;
                            send(UserId, "Set topic to: " + topic);
                            break;
                        }
                        catch (Exception ex)
                        {
                            send(UserId, "No topic provided!");
                            break;
                        }


                    case "/answer":
                        try
                        {
                            string answerToAdd = e.Message.Text.Split(' ')[1];
                            ConversationStates.Find(x => x.UserId == UserId).Answers.Add(answerToAdd);
                            send(UserId, "Added answer: " + answerToAdd);
                            break;
                        }
                        catch
                        {
                            send(UserId, "No answer provided!");
                            break;

                        }


                    case "/save":

                        ConversationState conversationState = ConversationStates.Find(x => x.UserId == UserId);
                        if (!(conversationState.Topic is null) && !(conversationState.Answers is null))
                        {

                        }
                        else
                        {
                            send(UserId, "Topic or Answers not set");
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
