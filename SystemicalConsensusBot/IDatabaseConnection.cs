using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemicalConsensusBot
{
    public interface IDatabaseConnection
    {
        Poll GetPoll(long id);
        Poll SavePoll(Poll poll);
        List<Poll> GetPollsByOwner(long ownerID);
    }
}
