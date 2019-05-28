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
            return $"";
        }

        public InlineKeyboardMarkup GetInlineKeyboardMarkup()
        {
            return null;
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

        public void Vote(int userID, int[] votes)
        {
            if (!IsLocked)
                ParticipantVotes[userID] = votes;
        }
    }
}
