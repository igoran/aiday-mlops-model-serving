using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Predictor.Models;

namespace Predictor.Services
{
    public class ApplicationInsightsClient : IMetricsClient
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly Uri _modelUri;

        public ApplicationInsightsClient(TelemetryConfiguration telemetryConfiguration, Uri modelUri)
        {
            _telemetryClient = new TelemetryClient(telemetryConfiguration);
            _telemetryClient.Context.Operation.Name = modelUri?.Segments?.LastOrDefault()?.ToString();
            _modelUri = modelUri;
        }

        public void Track(SentimentPrediction output, SentimentIssue input, ILogger logger)
        {
            try
            {
                string sentimentText = input.SentimentText;

                var props = new Dictionary<string, string>
                {
                    { "model_uri", _modelUri?.ToString() },
                    { "prediction_response", output.Prediction ? "Positive" : "Negative" },
                    { "prediction_text", sentimentText },
                };

                _telemetryClient.TrackMetric("Prediction.Probability", output.Probability, props);

                _telemetryClient.TrackMetric("Prediction.Score", output.Score, props);
            }
            catch(Exception ex)
            {
                // avoid fail prediction due to telemetry record saving issues
                logger?.LogError(ex, nameof(Track));
            }
        }
    }
}