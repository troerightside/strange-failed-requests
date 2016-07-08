using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace ReproducableIssue
{
    class EventualResponderActor : ReceiveActor
    {
        public Random Random { get; } = new Random();

        public IActorRef ReceivingQueueActor { get; set; }

        public EventualResponderActor(IActorRef receivingQueueActor)
        {
            this.ReceivingQueueActor = receivingQueueActor;

            Receive<SendResponseForMessage>((msg) =>
            {
                Context.System.Scheduler.ScheduleTellOnce(
                    TimeSpan.FromMilliseconds(Random.Next(100) + 200),
                    ReceivingQueueActor,
                    new ReceivingQueueActor.CompletedWorkMessage(msg.Id),
                    Self);
            });
        }

        public class SendResponseForMessage
        {
            public Guid Id { get; set; }

            public SendResponseForMessage(Guid id)
            {
                this.Id = id;
            }
        }
    }
}
