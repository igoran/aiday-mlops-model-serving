using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ML;
using Newtonsoft.Json;
using Predictor.Models;
using Predictor.Services;

namespace Predictor
{
    public class Funcs
    {
        private readonly PredictionEnginePool<SentimentIssue, SentimentPrediction> _predictionEnginePool;
        private readonly IMetricsClient _telemetryClient;

        public Funcs(PredictionEnginePool<SentimentIssue, SentimentPrediction> predictionEnginePool, IMetricsClient telemetryClient)
        {
            _predictionEnginePool = predictionEnginePool;
            _telemetryClient = telemetryClient;
        }

        [FunctionName("predictor")]
        public async Task<IActionResult> Predict(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "predict")] HttpRequest req, ILogger log)
        {
            //Parse HTTP Request Body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var sentimentIssue = JsonConvert.DeserializeObject<SentimentIssue>(requestBody);

            if (string.IsNullOrEmpty(sentimentIssue?.SentimentText)) {
                return new BadRequestResult();
            }

            //Make Prediction   
            var sentimentPrediction = _predictionEnginePool.Predict(modelName: Constants.ModelName, example: sentimentIssue);

            //Convert prediction to string
            string sentiment = Convert.ToBoolean(sentimentPrediction.Prediction) ? "Positive" : "Negative";

            _telemetryClient.Track(sentimentPrediction, sentimentIssue, log);

            //Return Prediction
            return new OkObjectResult(sentiment);
        }

        [FunctionName("predictorFull")]
        public async Task<IActionResult> PredictFull(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "predict-full")] HttpRequest req, ILogger log)
        {
            //Parse HTTP Request Body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var sentimentIssue = JsonConvert.DeserializeObject<SentimentIssue>(requestBody);

            if (string.IsNullOrEmpty(sentimentIssue?.SentimentText))
            {
                return new BadRequestResult();
            }

            //Make Prediction   
            var sentimentPrediction = _predictionEnginePool.Predict(modelName: Constants.ModelName, example: sentimentIssue);

            _telemetryClient.Track(sentimentPrediction, sentimentIssue, log);

            //Return Prediction
            return new OkObjectResult(sentimentPrediction?.ToString());
        }

        [FunctionName("predictorSmoke")]
        public IActionResult Smoke([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "smoke")] HttpRequest req, ILogger log)
        {
            try
            {
                var sentimentIssue = new SentimentIssue() { SentimentText = "This was a great place!" };

                //Make Prediction   
                var sentimentPrediction = _predictionEnginePool.Predict(modelName: Constants.ModelName, example: sentimentIssue);

                //Convert prediction to string
                string sentiment = Convert.ToBoolean(sentimentPrediction.Prediction) ? "Positive" : "Negative";

                //Get model uri
                var uri = Environment.GetEnvironmentVariable("ML_MODEL_URI") ?? string.Empty;
                
                //Return Prediction
                return new OkObjectResult($"{sentiment}-{uri}");
            }
            catch (Exception ex)
            {
                log.LogCritical(ex.ToString());
            }

            return new BadRequestResult();
        }

        [FunctionName("ping")]
        public IActionResult Ping([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ping")] HttpRequest req)
        {
            var uri = Environment.GetEnvironmentVariable("ML_MODEL_URI") ?? string.Empty;

            return new OkObjectResult($"Model Uri=\"{uri}\"");
        }
    }
}