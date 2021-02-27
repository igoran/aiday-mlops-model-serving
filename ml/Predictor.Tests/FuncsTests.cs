using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ML;
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

            var telemetryClient = testHost.ServiceProvider.GetRequiredService<IMetricsClient>();

            _sut = new Funcs(predictionEngine, telemetryClient);
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
            // arrange
            var req = new DefaultHttpRequest(new DefaultHttpContext());

            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new SentimentIssue() { SentimentText = "" }));

            req.Body = new MemoryStream(body);

            // act
            var result = await _sut.Predict(req, NullLogger.Instance);

            // assert
            result.ShouldBeOfType<BadRequestResult>();
        }

        [Theory]
        [MemberData(nameof(FeedbackScenario.Inputs), MemberType = typeof(FeedbackScenario))]
        public async Task Should_get_ok_result_and_good_predictions(string issue, bool expected)
        {
            // arrange
            var req = new DefaultHttpRequest(new DefaultHttpContext());

            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject( new SentimentIssue() { SentimentText = issue }) );

            req.Body = new MemoryStream(body);

            // act
            var result = (OkObjectResult) await _sut.Predict(req, NullLogger.Instance);

            // assert
            result.Value.ShouldBe(expected ? "Positive" : "Negative");
        }
    }
}