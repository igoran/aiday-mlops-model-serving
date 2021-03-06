using Predictor;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using System;
using System.Linq;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Predictor
{
    public class Utils
    {
        public static string CurrentModelVersionUri() => Environment.GetEnvironmentVariable("ML_MODEL_URI") ?? string.Empty;

        public static string CurrentModelVersion()
        {
            var value = CurrentModelVersionUri();

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                return "";
            }

            return uri?.Segments.LastOrDefault()?.Replace(".zip", "");
        }

    }
}
