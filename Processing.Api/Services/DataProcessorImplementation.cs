using Processing.Core;

namespace Processing.Api.Services
{
    public class DataProcessorImplementation : IDataProcessor
    {
        private readonly ILogger<DataProcessorImplementation> _logger;

        public DataProcessorImplementation(ILogger<DataProcessorImplementation> logger)
        {
            _logger = logger;
        }

        public async Task<ProcessResult> ProcessAsync(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
            _logger.LogWarning("Received empty or null data for processing.");
            return new ProcessResult(false, "Data is empty or null");
            }

            try
            {
            _logger.LogInformation($"Starting processing for data: {data}");
            // Your processing logic here
            await Task.Delay(100); // Simulate async processing
            _logger.LogInformation($"Successfully processed data: {data}");
            return new ProcessResult(true, "Processed successfully");
            }
            catch (Exception ex)
            {
            _logger.LogError(ex, $"Error occurred while processing data: {data}");
            return new ProcessResult(false, "Processing failed due to an error");
            }
        }
    }
}
