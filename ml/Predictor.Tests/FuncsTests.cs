using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ML;
using Moq;
using Newtonsoft.Json;
using Predictor.Models;
using Predictor.Services;
using Shouldly;
using Xunit;

namespace Predictor.Tests
{
    [Collection(TestsCollection.Name)]
    public class FuncsTests
    {
        readonly Funcs _sut;

        public FuncsTests(TestHost testHost)
        {
            var predictionEngine = testHost.ServiceProvider.GetRequiredService<PredictionEnginePool<SentimentIssue, SentimentPrediction>>();

            _sut = new Funcs(predictionEngine);
        }

        [Fact]
        public void Should_get_ok_from_smoke_test()
        {
            // arrange
            var req = new DefaultHttpRequest(new DefaultHttpContext());

            // act
            var result = _sut.Smoke(req, NullLogger.Instance);

            // assert
            result.ShouldBeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Should_get_bad_result_object_is_sentiment_text_is_null_or_empty()
        {
            var collector = new Mock<IAsyncCollector<string>>();

            // arrange
            var req = new DefaultHttpRequest(new DefaultHttpContext());

            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new SentimentIssue() { SentimentText = "" }));

            req.Body = new MemoryStream(body);

            // act
            var result = await _sut.Predict(req, collector.Object, CancellationToken.None, NullLogger.Instance);

            // assert
            result.ShouldBeOfType<BadRequestResult>();
        }

        [Fact]
        public void Should_Serialize_Data()
        {
            // arrange
            var response = File.ReadAllText("Payload.json");

            // act
            var result = _sut.PredictorResultCollector(response,NullLogger.Instance);

            // assert
            result.ShouldBeOfType<PredictionResult>();

            result.Text.ShouldBe("VERY BAD PLACE");
        }

        [Theory]
        [MemberData(nameof(FeedbackScenario.Inputs), MemberType = typeof(FeedbackScenario))]
        public async Task Should_get_ok_result_and_good_predictions(string issue, bool expected)
        {
            var collector = new Mock<IAsyncCollector<string>>();

            // arrange
            var req = new DefaultHttpRequest(new DefaultHttpContext());

            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject( new SentimentIssue() { SentimentText = issue }) );

            req.Body = new MemoryStream(body);

            // act
            var result = (JsonResult) await _sut.Predict(req, collector.Object, CancellationToken.None, NullLogger.Instance);

            result?.Value.ShouldBeAssignableTo<SentimentPrediction>();

            var value = result?.Value as SentimentPrediction;

            // assert
            value.Prediction.ShouldBe(expected);
        }
    }
}