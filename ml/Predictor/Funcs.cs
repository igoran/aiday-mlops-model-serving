using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ML;
using Newtonsoft.Json;
using Predictor.Models;

namespace Predictor
{
    public partial class Funcs
    {
        private readonly PredictionEnginePool<SentimentIssue, SentimentPrediction> _predictionEnginePool;

        public Funcs(PredictionEnginePool<SentimentIssue, SentimentPrediction> predictionEnginePool)
        {
            _predictionEnginePool = predictionEnginePool;
        }

        [FunctionName("predictor")]
        public async Task<IActionResult> Predict(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "predict")] HttpRequest req,
            [Queue(Constants.PredictionResponseQueue, Connection = Constants.StorageConnectionString)]
            IAsyncCollector<string> responseQueue,
            CancellationToken cancellationToken,
            ILogger log)
        {
            log.LogInformation($"{nameof(Predict)} function processed");

            //Parse HTTP Request Body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var sentimentIssue = JsonConvert.DeserializeObject<SentimentIssue>(requestBody);

            if (string.IsNullOrEmpty(sentimentIssue?.SentimentText)) {
                return new BadRequestResult();
            }

            //Make Prediction   
            var sentimentPrediction = _predictionEnginePool.Predict(modelName: Constants.ModelName, example: sentimentIssue);

            //Assign current model version
            sentimentPrediction.ModelVersion = Utils.CurrentModelVersion();

            //Put in queue result for retraining purpose
            await responseQueue.AddAsync(JsonConvert.SerializeObject(sentimentPrediction), cancellationToken);

            //Return Prediction
            return new JsonResult(sentimentPrediction);
        }

        [FunctionName("PredictorResultCollector")]
        [return: Table(Constants.PredictionResultTable, Connection = Constants.StorageConnectionString)]
        public PredictionResult PredictorResultCollector([QueueTrigger(Constants.PredictionResponseQueue, Connection = "AzureWebJobsStorage")] string response, ILogger log)
        {
            log.LogInformation($"{nameof(PredictorResultCollector)} function processed: {response}");

            try
            {
                var sentimentIssue = JsonConvert.DeserializeObject<SentimentPrediction>(response);

                return PredictionResult.From(sentimentIssue);
            }
            catch (Exception ex)
            {
                log.LogCritical(nameof(PredictorResultCollector), ex);

                throw;
            }
        }

        [FunctionName("PredictorResultDownload")]
        public async Task<IActionResult> PredictorResultDownload(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "predict/download/{modelVersion?}")] HttpRequest req,
            string modelVersion,
            [Table(Constants.PredictionResultTable, Connection = Constants.StorageConnectionString)] CloudTable table,
            ILogger log)
        {
            log.LogInformation($"{nameof(PredictorResultDownload)} modelVersion: {modelVersion}");

            //Get Model Version
            var version = modelVersion ?? Utils.CurrentModelVersion();

            var result = await PredictionResult.ExportTableAsCsv<PredictionResult>(table, modelVersion);

            //Return Prediction
            return new FileContentResult(result, "application/octet-stream") { FileDownloadName = $"{version}_{DateTime.Now:s}.csv" };
        }

        [FunctionName("PredictorSmoke")]
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

        [FunctionName("PredictorPing")]
        public IActionResult Ping([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ping")] HttpRequest req)
        {
            var uri = Utils.CurrentModelVersionUri();

            return new OkObjectResult(uri);
        }
    }
}