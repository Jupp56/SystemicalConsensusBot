﻿using Newtonsoft.Json;
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
        public bool IsLocked { get; private set; }
        [JsonProperty]
        public string[] Answers { get; }
        [JsonProperty]
        private Dictionary<int, int[]> ParticipantVotes { get; } = new Dictionary<int, int[]>();
        [JsonProperty]
        public string Topic { get; }

#pragma warning disable IDE0051
        [JsonConstructor]
        private Poll(long pollID, int ownerId, bool isLocked, string[] answers, Dictionary<int, int[]> participantVotes, string topic)
        {
            PollID = pollID;
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

            for (int i = 0; i < Answers.Count(); i++)
            {
                double result = 0;

                foreach (var kvp in ParticipantVotes)
                {
                    result += kvp.Value[i];

                }

                results.Add(result / Answers.Count());
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

        public bool Vote(int userID, int answerId, int change, out int newValue)
        {
            if (!IsLocked)
            {
                ParticipantVotes[userID][answerId] += change;
                newValue = ParticipantVotes[userID][answerId];
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
