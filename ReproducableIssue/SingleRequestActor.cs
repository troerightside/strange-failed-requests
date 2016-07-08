using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace ReproducableIssue
{
    class SingleRequestActor : ReceiveActor
    {
        public IActorRef ReceivingQueueActor { get; set; }
        public IActorRef EventualResponderActor { get; set; }

        public Dictionary<Guid, IActorRef> InProcessRequests { get; }

        public SingleRequestActor(IActorRef receivingQueueActor, IActorRef eventualResponder)
        {
            InProcessRequests = new Dictionary<Guid, IActorRef>();
            this.ReceivingQueueActor = receivingQueueActor;
            this.EventualResponderActor = eventualResponder;

            Receive<StartNewRequestMessage>((msg) =>
            {
                Guid thisRequest = Guid.NewGuid();
                InProcessRequests[thisRequest] = Sender;
                ReceivingQueueActor.Tell(new ReceivingQueueActor.RegisterReceiveMessage(thisRequest, Self));
                EventualResponderActor.Tell(new EventualResponderActor.SendResponseForMessage(thisRequest));
            });

            Receive<FinishedMessage>((msg) =>
            {
                if (!InProcessRequests.ContainsKey(msg.Id))
                {
                    return;
                }

                InProcessRequests[msg.Id].Tell(new DoneMessage());
                InProcessRequests.Remove(msg.Id);
            });
        }

        public class StartNewRequestMessage
        {
        }

        public class FinishedMessage
        {
            public Guid Id { get; set; }
            public FinishedMessage(Guid id)
            {
                this.Id = id;
            }
        }
    }
}
