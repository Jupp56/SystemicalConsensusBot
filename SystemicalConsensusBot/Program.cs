using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace SystemicalConsensusBot
{
    class Program
    {
        IDatabaseConnection Connection;
        static void Main(string[] args)
        {

        }

        public void Vote(long pollId, int[] votes)
        {

            Poll poll = Connection.GetPoll(pollId);




        }
    }
}
