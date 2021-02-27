using System.Collections.Generic;

namespace Predictor.Tests
{
    public class FeedbackScenario
    {
        public static IEnumerable<object[]> Inputs =>
            new List<object[]>
            {
                new object[] { "A great touch!", true},
                new object[] {"I highly recommend this place", true},
                new object[]{"Not enjoyed, I don't recommend this place",false},
                new object[] { "Would not go back",false},
            };
    }
}
