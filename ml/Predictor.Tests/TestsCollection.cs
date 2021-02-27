using Xunit;

namespace Predictor.Tests
{
    [CollectionDefinition(Name)]
    public class TestsCollection : ICollectionFixture<TestHost>
    {
        public const string Name = nameof(TestsCollection);
    }
}
