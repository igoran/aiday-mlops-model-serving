using Predictor;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Predictor
{
    public class Constants
    {
        public const string ModelName = "SentimentAnalysisModel";

        public const string PredictionResponse = "prediction-response";
    }

    public class Utils
    {
        public  static string CurrentModelVersionUri() => Environment.GetEnvironmentVariable("ML_MODEL_URI") ?? string.Empty;
    }
}
