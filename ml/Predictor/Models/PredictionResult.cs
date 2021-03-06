using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Predictor.Models
{
    public class PredictionResult : TableEntity
    {
        public string Text { get; set; }
        public int Response { get; set; }
        public double Probability { get; set; }
        public double Score { get; set; }

        public static PredictionResult From(SentimentPrediction prediction)
        {
            var predictionResult = new PredictionResult
            {
                PartitionKey = prediction.ModelVersion,
                RowKey = Guid.NewGuid().ToString(),
                Timestamp = DateTimeOffset.Now,
                Probability = prediction.Probability,
                Score = prediction.Score,
                Text = prediction.SentimentText,
                Response = prediction.Prediction ? 1 : 0
            };

            return predictionResult;
        }

        public static async Task<byte[]> ExportTableAsCsv<T>(CloudTable table, string partitionKey)
            where T : ITableEntity, new()
        {
            var list = await GetEntitiesFromTable<PredictionResult>(table, partitionKey);

            var sb = new StringBuilder();

            foreach (var result in list)
            {
                sb.Append(result.ToCsv());
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private string ToCsv()
        {
            return $"{Text}|{Probability}|{Score}|{PartitionKey}" + Environment.NewLine;
        }

        public static async Task<IEnumerable<T>> GetEntitiesFromTable<T>(CloudTable table, string partitionKey) where T : ITableEntity, new()
        {
            TableQuerySegment<T> querySegment = null;
            var entities = new List<T>();
            var query = new TableQuery<T>().Where(
                TableQuery.GenerateFilterCondition(
                    "PartitionKey",
                    QueryComparisons.GreaterThanOrEqual,
                    partitionKey
                )
            );

            do
            {
                querySegment = await table.ExecuteQuerySegmentedAsync(query, querySegment?.ContinuationToken);
                entities.AddRange(querySegment.Results);
            } while (querySegment.ContinuationToken != null);

            return entities;
        }
    }
}
