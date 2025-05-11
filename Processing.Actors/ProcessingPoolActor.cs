using Akka.Actor;
using Akka.Dispatch;
using Akka.Routing;
using Microsoft.Extensions.Logging;
using Processing.Core;
using System;

namespace Processing.Actors
{
    public class ProcessingPoolActor : ReceiveActor
    {
        private readonly IActorRef _router;

        public ProcessingPoolActor(IDataProcessor processor,
                                ILogger<DataProcessorActor> logger,
                                int poolSize = 10)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (poolSize <= 0)
                throw new ArgumentException("Pool size must be positive", nameof(poolSize));

            // Create router configuration
            var routerConfig = new RoundRobinPool(
                nrOfInstances: poolSize,
                resizer: null,
                supervisorStrategy: DefaultSupervisorStrategy(), // Use custom strategy method
                routerDispatcher: Dispatchers.DefaultDispatcherId,
                usePoolDispatcher: false);

            var props = Props.Create(() => new DataProcessorActor(processor, logger))
                .WithRouter(routerConfig);

            _router = Context.ActorOf(props, "processing-router");

            Receive<object>(message => _router.Forward(message));
        }

        private static SupervisorStrategy DefaultSupervisorStrategy()
        {
            return new OneForOneStrategy(
                maxNrOfRetries: 10,
                withinTimeRange: TimeSpan.FromMinutes(1),
                localOnlyDecider: ex =>
                {
                    // Handle specific exceptions here
                    return Directive.Restart;
                });
        }
    }
}