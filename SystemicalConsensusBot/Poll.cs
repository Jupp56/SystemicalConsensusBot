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
        public Dictionary<int, int[]> ParticipantVotes { get; } = new Dictionary<int, int[]>();

        public Poll(int ownerId)
        {
            this.OwnerId = ownerId;
        }

    }
}
