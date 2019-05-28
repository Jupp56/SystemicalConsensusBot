using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace SystemicalConsensusBot
{
    [JsonObject(MemberSerialization = MemberSerialization.OptOut)]
    public class Poll
    {
        [JsonProperty]
        public long PollID { get; internal set; } = -1;
        [JsonProperty]
        public int OwnerId { get; }
        [JsonProperty]
        public int AnswerCount { get; }
        [JsonProperty]
        public bool IsLocked { get; private set; }
        [JsonProperty]
        public string[] Answers { get; }
        [JsonProperty]
        private Dictionary<int, int[]> ParticipantVotes { get; } = new Dictionary<int, int[]>();
        [JsonProperty]
        public string Topic { get; }

        [JsonConstructor]
        private Poll() { }

        public Poll(string topic, int ownerId, int answerCount, string[] answers)
        {
            this.Topic = topic;
            this.OwnerId = ownerId;
            this.AnswerCount = answerCount;
            this.Answers = answers;
        }

        public string GetPollMessage()
        {
            string message = $"Poll:\n<b>{Topic}</b>\n\nYou can answer:\n\n";
            for(int i = 0; i<Answers.Length; i++)
            {
                message += $"\n{i}: {Answers[i]}";
            }

            return message;
        }

        public InlineKeyboardMarkup GetInlineKeyboardMarkup()
        {
            if (IsLocked) return null;
            List<InlineKeyboardButton[]> rows = new List<InlineKeyboardButton[]>();
            int counter = 0;
            foreach (var answer in Answers)
            {
                InlineKeyboardButton[] row = new InlineKeyboardButton[3];
                row[0] = new InlineKeyboardButton { CallbackData = $"vote:{PollID}:{counter}:-", Text = "-" };
                row[1] = new InlineKeyboardButton { CallbackData = "null", Text = $"{counter}." };
                row[2] = new InlineKeyboardButton { CallbackData = $"vote:{PollID}:{counter}:+", Text = "+" };
                rows.Add(row);
                counter++;
            }
            InlineKeyboardButton[] lastRow = { new InlineKeyboardButton { CallbackData = $"show:{PollID}", Text = "Show my votes" },
                new InlineKeyboardButton { CallbackData = $"close:{PollID}", Text = "Close poll" } };
            rows.Add(lastRow);
            return new InlineKeyboardMarkup(rows.ToArray());
        }

        private double[] ComputeResult()
        {
            List<double> results = new List<double>();

            for (int i = 0; i < AnswerCount; i++)
            {
                double result = 0;

                foreach (var kvp in ParticipantVotes)
                {
                    result += kvp.Value[i];

                }

                results.Add(result / AnswerCount);
            }

            return results.ToArray();
        }

        public double[] GetPollResults()
        {
            return ComputeResult();
        }

        public void Lock()
        {
            IsLocked = true;
        }

        public bool Vote(int userID, int answerId, int change)
        {
            if (!IsLocked)
            {
                ParticipantVotes[userID][answerId] += change;
                return true;
            }
            else
            {
                return false;
            }
            
        }
    }
}
