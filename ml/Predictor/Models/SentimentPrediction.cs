using Microsoft.ML.Data;

namespace Predictor.Models
{
    public class SentimentPrediction : SentimentIssue
    {
        [ColumnName("PredictedLabel")]
        public bool Prediction { get; set; }

        public float Probability { get; set; }

        public float Score { get; set; }

        public override string ToString()
        {
            return $"\"{SentimentText}\" Positive: '{Prediction}' {nameof(Score)}: {Score} {nameof(Probability)}: {Probability}";
        }
    }
}
