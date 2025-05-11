using Akka.Actor;
using Akka.Remote;
using Microsoft.AspNetCore.Mvc;
using Processing.Core;
using System.Threading.Tasks;
using System.Transactions;
using static Processing.Core.Messages;

namespace Processing.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProcessingController : ControllerBase
    {
        private readonly IActorRef _processor;
        private readonly ILogger<ProcessingController> _logger;
        private readonly ActorSystem _actorSystem;

        public ProcessingController(
            IActorRef processor,
            ILogger<ProcessingController> logger,
            ActorSystem actorSystem)
        {
            _processor = processor;
            _logger = logger;
            _actorSystem = actorSystem;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessData([FromBody] ProcessDataRequest request)
        {
            try
            {
                _logger.LogInformation("Processing request {RequestId}", request.RequestId);

                // Proper async pattern with Ask pattern
                var result = await _processor.Ask<ProcessDataResponse>(request, TimeSpan.FromSeconds(5));

                return Ok(result);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Request {RequestId} timed out", request.RequestId);
                return StatusCode(StatusCodes.Status504GatewayTimeout);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request {RequestId}", request.RequestId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("transaction")]
        public async Task<IActionResult> ProcessTransaction([FromBody] TransactionMessage transaction)
        {
            try
            {
                _logger.LogInformation("Processing transaction {TransactionId}", transaction.TransactionId);

                // Option 1: Fire-and-forget with proper async signaling
                _processor.Tell(transaction);
                await Task.Yield(); // Ensures async context

                return Accepted(new TransactionResponse
                {
                    TransactionId = transaction.TransactionId.ToString(),
                    Status = "Accepted",
                    Message = "Transaction is being processed",
                    Timestamp = DateTime.UtcNow
                });

                // Option 2: Request-response version (uncomment if you need a response)

                

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing transaction {TransactionId}", transaction.TransactionId);

                return StatusCode(StatusCodes.Status500InternalServerError, new TransactionErrorResponse
                {
                    TransactionId = transaction.TransactionId.ToString(),
                    Error = "Processing failed",
                    Details = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // Supporting classes
        

        [HttpGet("status/{requestId}")]
        public async Task<IActionResult> GetStatus(string requestId)
        {
            try
            {
            var response = await _processor.Ask<object>(
                new GetStatus(requestId),
                TimeSpan.FromSeconds(15)); // Increased timeout to 15 seconds

            return response switch
            {
                ProcessingStatus status => Ok(status),
                Core.ProcessingError error => BadRequest(new { Error = error.ErrorMessage }), // Simplified namespace
                _ => StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = $"Unexpected response type: {response.GetType()}" })
            };
            }
            catch (TimeoutException)
            {
            _logger.LogWarning("Status check for {RequestId} timed out", requestId);
            return StatusCode(StatusCodes.Status504GatewayTimeout);
            }
            catch (Exception ex)
            {
            _logger.LogError(ex, "Error checking status for {RequestId}", requestId);
            return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}