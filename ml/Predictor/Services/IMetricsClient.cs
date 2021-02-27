using Microsoft.Extensions.Logging;
using Predictor.Models;

namespace Predictor.Services
{
    public interface IMetricsClient
    {
        void Track(SentimentPrediction prediction, SentimentIssue data, ILogger logger = default);
    }
}