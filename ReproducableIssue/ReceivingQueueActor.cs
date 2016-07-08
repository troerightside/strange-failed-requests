using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace ReproducableIssue
{
    public class ReceivingQueueActor : ReceiveActor
    {
        public Dictionary<Guid, IActorRef> States { get; set; }

        public ReceivingQueueActor()
        {
            States = new Dictionary<Guid, IActorRef>();

            Receive<RegisterReceiveMessage>((msg) =>
            {
                States[msg.Id] = msg.SendToMe;
            });

            Receive<CompletedWorkMessage>((msg) =>
            {
                if (!States.ContainsKey(msg.Id))
                {
                    Console.WriteLine("Arrived too fast");
                    return;
                }

                States[msg.Id].Tell(new SingleRequestActor.FinishedMessage(msg.Id));
                States.Remove(msg.Id);
            });
        }

        #region Messages

        public class CompletedWorkMessage
        {
            public Guid Id { get; set; }

            public CompletedWorkMessage(Guid id)
            {
                this.Id = id;
            }
        }

        public class RegisterReceiveMessage
        {
            public Guid Id { get; set; }

            public IActorRef SendToMe { get; set; }

            public RegisterReceiveMessage(Guid id, IActorRef sendToMe)
            {
                this.Id = id;
                this.SendToMe = sendToMe;
            }
        }

        #endregion
    }
}
