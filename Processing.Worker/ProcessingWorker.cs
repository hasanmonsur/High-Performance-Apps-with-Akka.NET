using Akka.Actor;
using Akka.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Processing.Actors;
using Processing.Core;
using static Processing.Core.Messages;

namespace Processing.Worker
{
    public class ProcessingWorker : BackgroundService
    {
        private readonly ActorSystem _actorSystem;
        private readonly IActorRef _processor;
        private readonly ILogger<ProcessingWorker> _logger;

        public ProcessingWorker(
                ActorSystem actorSystem,
                ILogger<ProcessingWorker> logger,
                IServiceProvider serviceProvider)
            {
                _actorSystem = actorSystem ?? throw new ArgumentNullException(nameof(actorSystem));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));

                // Use Akka.NET DependencyResolver to create the actor
                var resolver = DependencyResolver.For(_actorSystem);
                _processor = _actorSystem.ActorOf(
                    resolver.Props<DataProcessorActor>(),
                    "processor"
                );
            }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ProcessingWorker started.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var message = await FetchNextMessageAsync(stoppingToken);
                    if (message != null)
                    {
                        _processor.Tell(message);
                    }
                    await Task.Delay(100, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ProcessingWorker is shutting down gracefully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in ProcessingWorker.");
            }
        }

        private async Task<ProcessDataRequest?> FetchNextMessageAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Simulate fetching a message from a queue
                var messageContent = "Hello Bangladesh";
                var requestId = Guid.NewGuid();

                // Create a sample message
                var message = new ProcessDataRequest(requestId, messageContent);

                // Simulate async delay (like real network I/O)
                await Task.Delay(100, stoppingToken);

                return message;
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch message.");
                return null;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ProcessingWorker is stopping.");
            await base.StopAsync(cancellationToken);
        }
    }
}