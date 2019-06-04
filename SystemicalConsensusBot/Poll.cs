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
        public long PollId { get; internal set; } = -1;
        [JsonProperty]
        public int OwnerId { get; }
        [JsonProperty]
        public bool IsLocked { get; private set; }
        [JsonProperty]
        public string[] Answers { get; }
        [JsonProperty]
        private Dictionary<int, int[]> ParticipantVotes { get; } = new Dictionary<int, int[]>();
        [JsonProperty]
        public string Topic { get; }

#pragma warning disable IDE0051
        [JsonConstructor]
        private Poll(long pollId, int ownerId, bool isLocked, string[] answers, Dictionary<int, int[]> participantVotes, string topic)
        {
            PollId = pollId;
            OwnerId = ownerId;
            IsLocked = isLocked;
            Answers = answers;
            ParticipantVotes = participantVotes;
            Topic = topic;
        }
#pragma warning restore IDE0051

        public Poll(string topic, int ownerId, string[] answers)
        {
            this.Topic = topic;
            this.OwnerId = ownerId;
            this.Answers = answers;
        }

        public string GetPollMessage()
        {
            if (!IsLocked)
            {
                string message = $"Poll:\n<b>{Topic}</b>\n\nYou can answer:\n\n";
                for (int i = 0; i < Answers.Length; i++)
                {
                    message += $"\n{i}: {Answers[i]}";
                }

                return message;
            }
            else
            {
                string message = $"Poll:\n<b>{Topic}</b>\n\nResults are:\n";

                if (ParticipantVotes.Count > 0)
                {
                    
                    double[] results = ComputeResult();
                    for (int i = 0; i < Answers.Length; i++)
                    {
                        bool winner = results.OrderBy(x => x).FirstOrDefault() == results[i];
                        message += $"\n{(winner ? "<i>" : "")}{i}. {Answers[i]}: {results[i]:N2}{(winner ? "</i>" : "")}";
                    }

                    message += $"\n\nIn total, {ParticipantVotes.Count.ToString()} people participated!\n";

                    return message;
                }
                else
                {
                    message += "\nSadly, nobody has voted, so no results to display here :-(";

                    return message;
                }
            }
        }

        public InlineKeyboardMarkup GetInlineKeyboardMarkup()
        {
            if (IsLocked) return null;
            List<InlineKeyboardButton[]> rows = new List<InlineKeyboardButton[]>();
            int counter = 0;
            foreach (var answer in Answers)
            {
                InlineKeyboardButton[] row = new InlineKeyboardButton[3]
                {
                    new InlineKeyboardButton { CallbackData = $"vote:{PollId}:{counter}:-", Text = "-" },
                    new InlineKeyboardButton { CallbackData = $"showone:{PollId}:{counter}", Text = $"{counter}." },
                    new InlineKeyboardButton { CallbackData = $"vote:{PollId}:{counter}:+", Text = "+" }
                };

                rows.Add(row);
                counter++;
            }
            InlineKeyboardButton[] lastRow = { new InlineKeyboardButton { CallbackData = $"show:{PollId}", Text = "Show my votes" },
                new InlineKeyboardButton { CallbackData = $"close:{PollId}", Text = "Close poll" } };
            rows.Add(lastRow);
            InlineKeyboardButton[] lastlastRow = { new InlineKeyboardButton { Url = Program.HelpLink, Text = "Help" } };
            rows.Add(lastlastRow);
            return new InlineKeyboardMarkup(rows.ToArray());
        }

        private double[] ComputeResult()
        {
            List<double> results = new List<double>();

            for (int i = 0; i < Answers.Count(); i++)
            {
                double result = 0;

                foreach (var kvp in ParticipantVotes)
                {
                    result += kvp.Value[i];

                }

                results.Add(result / ParticipantVotes.Count);
            }

            return results.ToArray();
        }

        public int[] GetUserVotes(int userId)
        {
            if (!ParticipantVotes.ContainsKey(userId))
                ParticipantVotes[userId] = new int[Answers.Length];
            return ParticipantVotes[userId];
        }

        public void Lock()
        {
            IsLocked = true;
        }

        public bool Vote(int userID, int answerId, int change, out int newValue)
        {
            if (!IsLocked)
            {
                if (!ParticipantVotes.ContainsKey(userID)) ParticipantVotes[userID] = new int[Answers.Length];
                ParticipantVotes[userID][answerId] += change;
                newValue = ParticipantVotes[userID][answerId];
                if (newValue < 0) newValue = ParticipantVotes[userID][answerId] = 0;
                if (newValue > 10) newValue = ParticipantVotes[userID][answerId] = 10;
                return true;
            }
            else
            {
                newValue = -1;
                return false;
            }

        }
    }
}