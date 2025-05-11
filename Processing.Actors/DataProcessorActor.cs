using Akka.Actor;
using Akka.Pattern;
using Akka.Routing;
using Microsoft.Extensions.Logging;
using Processing.Core;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using static Processing.Core.Messages;

namespace Processing.Actors
{
    public class DataProcessorActor : ReceiveActor
    {
        private readonly IDataProcessor _processor;
        private readonly ILogger<DataProcessorActor> _logger;
        private readonly CircuitBreaker _circuitBreaker;

        public DataProcessorActor(IDataProcessor processor, ILogger<DataProcessorActor> logger)
        {
            _processor = processor;
            _logger = logger;

            _circuitBreaker = new CircuitBreaker(
                scheduler: Context.System.Scheduler,
                maxFailures: 5,
                callTimeout: TimeSpan.FromSeconds(30),
                resetTimeout: TimeSpan.FromSeconds(60));

            _circuitBreaker.OnOpen(() => _logger.LogWarning("Circuit breaker opened!"));
            _circuitBreaker.OnHalfOpen(() => _logger.LogInformation("Circuit breaker half-open"));
            _circuitBreaker.OnClose(() => _logger.LogInformation("Circuit breaker closed"));

            Receive<ProcessDataRequest>(HandleProcessRequest);
            Receive<GetStatus>(HandleGetStatus);
        }

        private void HandleProcessRequest(ProcessDataRequest request)
        {
            _circuitBreaker.WithCircuitBreaker(async () =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    var result = await _processor.ProcessAsync(request.Data);
                    sw.Stop();
                    _logger.LogInformation("Processed request {RequestId} in {ElapsedMs}ms", request.RequestId, sw.ElapsedMilliseconds);
                    return new ProcessDataResponse(request.RequestId, result, sw.Elapsed);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    _logger.LogError(ex, "Processing failed for request {RequestId}", request.RequestId);
                    throw;
                }
            })
            .PipeTo(
                recipient: Sender,
                success: response => response,
                failure: ex => new Messages.ProcessingError(request.Data, ex.Message)
            );
        }

        private void HandleGetStatus(GetStatus request)
        {
            _logger.LogDebug("Received GetStatus for {RequestId}", request.RequestId);
            if (string.IsNullOrEmpty(request.RequestId))
            {
                _logger.LogWarning("Invalid request ID for GetStatus: {RequestId}", request.RequestId);
                Sender.Tell(new Messages.ProcessingError(request.RequestId, "Invalid request ID"));
            }
            else
            {
                // Replace with actual status check (e.g., database, cache)
                _logger.LogInformation("Retrieved status for {RequestId}", request.RequestId);
                Sender.Tell(new ProcessingStatus { Status = "Completed" });
            }
        }
    }
}