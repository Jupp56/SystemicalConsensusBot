using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemicalConsensusBot
{
    class ConversationState
    {
        public int UserId { get; }
        public string Topic { get; set; }
        public List<string> Answers = new List<string>();
        public InteractionStates InteractionState { get; set; }
        public DateTime LastChanged { get; set; } = DateTime.Now;

        public ConversationState(int userId)
        {
            this.UserId = userId;
        }
        public enum InteractionStates
        {
            notstarted,
            started,
            parametersSend,
            finished
        }
    }
}
