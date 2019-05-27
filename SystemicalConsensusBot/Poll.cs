using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemicalConsensusBot
{
    public class Poll
    {
        public long PollID { get; internal set; } = -1;
        public int OwnerId { get; }
        public int AnswerCount { get; }
        public bool IsLocked { get; private set; }
        public Dictionary<int, int[]> ParticipantVotes { get; } = new Dictionary<int, int[]>();

        public Poll(int ownerId, int answerCount)
        {
            this.OwnerId = ownerId;
            this.AnswerCount = answerCount;
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
    }
}
