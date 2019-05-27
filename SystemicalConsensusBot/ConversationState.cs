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

        public string Topic { get { return this.Topic; } set { this.Topic = value; LastChanged = DateTime.Now; } }

        public List<string> Answers = new List<string>();

        public InteractionStates InteractionState { get { return InteractionState; } set { this.InteractionState = value; LastChanged = DateTime.Now; } }

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
