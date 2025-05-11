using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Processing.Core
{
    // Option 1: Add required modifier (.NET 7+)
    public record TransactionResult
    {
        public required string TransactionId { get; init; }
        public required string ErrorMessage { get; init; }
        public bool IsSuccessful { get; init; }
    }

    
    public record ProcessResult(bool Success, string Message);
    public class GetStatus
    {
        public string RequestId { get; }
        public GetStatus(string requestId) => RequestId = requestId;
    }

    

    public class ProcessingStatus
    {
        public string Status { get; set; } // e.g., "Completed", "Pending"
    }

      
    public class Messages
    {
        public class ProcessingError
        {
            public string RequestId { get; }
            public string Error { get; } // Matches error message context
            public ProcessingError(string requestId, string error) => (RequestId, Error) = (requestId, error);
        }
    }
}
