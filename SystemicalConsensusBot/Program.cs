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

        private static Dictionary<int, InteractionStates> ConversationStates { get; set; } = new Dictionary<int, InteractionStates>();
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


        enum InteractionStates
        {
            notstarted,
            started,
            parametersSend,
            finished
        }

        #region BotEventHandlers
        private static void BotOnMessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Message.Chat.Type != ChatType.Private) return;

            switch (e.Message.Text)
            {
                case ("/start"):
                    ConversationStates[e.Message.From.Id] = InteractionStates.started;
                    break;


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
