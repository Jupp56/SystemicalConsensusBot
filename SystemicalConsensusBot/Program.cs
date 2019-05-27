using System;
using System.Collections.Generic;
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

        private IDatabaseConnection databaseConnection;

        private static List<ConversationState> ConversationStates { get; set; } = new List<ConversationState>();
        static void Main(string[] args)
        {
            Bot = new TelegramBotClient(databaseConnection.GetBotToken());

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


        

        #region BotEventHandlers
        private static void BotOnMessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Message.Chat.Type != ChatType.Private) return;

            if (e.Message.Entities[0].Type == MessageEntityType.BotCommand)
            {
                switch (e.Message.EntityValues.ElementAt(0))
                {
                    case "/start":

                        Bot.SendTextMessageAsync(e.Message.From.Id, "Welcome to Systemical_Consensus Bot, the bot that finally decides: Where do we wanna eat?\nTo proceed, please at first choose the desired topic of your poll with \"/topic <topic>\".\n To add answers, send \"/answer <answer>\"");

                        if (ConversationStates.Exists(x => x.UserId == e.Message.From.Id)){
                            ConversationStates.Find(x => x.UserId == e.Message.From.Id).InteractionState = ConversationState.InteractionStates.started;
                        }

                        else
                        {
                            ConversationStates.Add(new ConversationState(e.Message.From.Id));
                        }
                        break;

                    case "/cancel":
                    case "/stop":
                      
                       //delete here
                        break;

                    case "/topic":
                        
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
