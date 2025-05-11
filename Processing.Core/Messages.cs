namespace Processing.Core;

public record TransactionMessage(
       Guid TransactionId,
       string AccountFrom,
       string AccountTo,
       decimal Amount,
       DateTime Timestamp);

public record ProcessDataRequest(Guid RequestId, string Data);
public record ProcessDataResponse(Guid RequestId, object Result, TimeSpan ProcessingTime);
public record ProcessingError(Guid RequestId, string ErrorMessage);

public record TransactionResponse
{
            public string TransactionId { get; init; } = string.Empty;
            public string Status { get; init; } = string.Empty;
            public string Message { get; init; } = string.Empty;
            public DateTime Timestamp { get; init; }
        }

        public record TransactionErrorResponse
        {
            public string TransactionId { get; init; } = string.Empty;
            public string Error { get; init; } = string.Empty;
            public string Details { get; init; } = string.Empty;
            public DateTime Timestamp { get; init; }
        }