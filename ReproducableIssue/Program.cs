using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;

namespace ReproducableIssue
{
    class Program
    {
        static void Main(string[] args)
        {
            ThreadPool.SetMaxThreads(200, 200);
            ThreadPool.SetMinThreads(200, 200);

            var system = ActorSystem.Create("TestingSystem");

            // in real code, this is an actor that collects messages from a message queueing
            // system, and redirects them to the original, requesting actor.
            var receivingQueueActor = system.ActorOf(
                Props.Create<ReceivingQueueActor>(),
                nameof(ReceivingQueueActor));

            // latency simulator
            var eventualResponseActor = system.ActorOf(
                Props.Create(() => new EventualResponderActor(receivingQueueActor)),
                nameof(EventualResponderActor));

            // pool of actors that are making the actual requests that will have a response
            // returned to them via the message queueing systme.
            var pool = system.ActorOf(
                Props.Create(() => new SingleRequestActor(
                    receivingQueueActor,
                    eventualResponseActor)).WithRouter(
                        new SmallestMailboxPool(
                            100,
                            new DefaultResizer(1, 150),
                            SupervisorStrategy.DefaultStrategy,
                            "akka.actor.smallest-mailbox-pool")));

            // this commented-out code works and prints "Request failed" 
            //foreach (var a in CauseInvocationForever())
            //{
            //    if (a == 1)
            //    {
            //        Console.WriteLine("Still working");
            //    }

            //    var request = pool.Ask(new SingleRequestActor.StartNewRequestMessage());
            //    request.Wait(2000);
            //    if (!request.IsCompleted)
            //    {
            //        Console.WriteLine("Request failed");
            //    }
            //    else
            //    {
            //        Console.WriteLine("Request succeeded.");
            //    }
            //}

            // this code frequently prints "Request failed"
            Parallel.ForEach(CauseInvocationForever(), (a) =>
            {
                if (a == 1)
                {
                    Console.WriteLine("Still working");
                }

                var request = pool.Ask(new SingleRequestActor.StartNewRequestMessage());
                request.Wait(2000);
                if (!request.IsCompleted)
                {
                    Console.WriteLine("Request failed");
                }
                else
                {
                    Console.WriteLine("Request succeeded.");
                }
            }
            );
        }

        // just an enumerable to have the system run forever, and occassionally print
        // out a status report.
        private static IEnumerable<int> CauseInvocationForever()
        {
            int i = 0;
            while (true)
            {
                i++;
                if (i == 100)
                {
                    i = 0;
                    yield return 1;
                }
                else
                {
                    yield return 0;
                }
            }
        }
    }
}
