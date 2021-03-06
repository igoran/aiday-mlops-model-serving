using Predictor;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;


namespace Predictor
{
    public class Constants
    {
        public const string ModelName = "SentimentAnalysisModel";

        public const string PredictionResponseQueue = "PredictionResponseQueue";
        public const string PredictionResultTable = "PredictionResultsTable";
        public const string StorageConnectionString = "AzureWebJobsStorage";
    }
}
