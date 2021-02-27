using Predictor;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Predictor
{
    public class Constants
    {
        public const string ModelName = "SentimentAnalysisModel";
    }
}
