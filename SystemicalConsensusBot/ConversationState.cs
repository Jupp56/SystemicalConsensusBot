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

        public string Topic { get { return this.InternalTopic; } set { this.InternalTopic = value; LastChanged = DateTime.Now; } }
        private string InternalTopic;
        public List<string> Answers = new List<string>();

        public InteractionStates InteractionState { get { return InternalInteractionState; } set { this.InternalInteractionState = value; LastChanged = DateTime.Now; } }
        private InteractionStates InternalInteractionState;
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
