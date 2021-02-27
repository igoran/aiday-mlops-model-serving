using System;
using Pulumi;
using Pulumi.Azure.AppService;
using Pulumi.Azure.Storage;
using Pulumi.Azure.Core;
using Pulumi.Azure.AppService.Inputs;
using Pulumi.Azure.AppInsights;

namespace infra
{
    public class MyFunctionApp : FunctionApp
    {
        public MyFunctionApp(string name, ResourceGroup resourceGroup, Plan appServicePlan, Account storageAccount, InputMap<string> settings) : base( name, new FunctionAppArgs()
        {
            ResourceGroupName = resourceGroup.Name,
            AppServicePlanId = appServicePlan.Id,
            StorageAccountName = storageAccount.Name,
            StorageAccountAccessKey = storageAccount.PrimaryAccessKey,
            Version = "~3",
            AppSettings = settings,
            SiteConfig = new FunctionAppSiteConfigArgs
            {
                Cors = new FunctionAppSiteConfigCorsArgs
                {
                    AllowedOrigins = new InputList<string> { "http://localhost:5500" },
                    SupportCredentials = true
                }
            }
        } )
        {

        }

        public static InputMap<string> CreateAppSetting(string appPackageUrl, Account storageAccount, Insights appInsights)
        {
            if (string.IsNullOrWhiteSpace(appPackageUrl))
            {
                throw new ArgumentException($"'{nameof(appPackageUrl)}' cannot be null or whitespace", nameof(appPackageUrl));
            }

            return new InputMap<string>()
            {
                    {"runtime", "dotnet"},
                    {"WEBSITE_RUN_FROM_PACKAGE", appPackageUrl},
                    {"AzureWebJobsStorage", storageAccount.PrimaryConnectionString},
                    {"APPINSIGHTS_INSTRUMENTATIONKEY", appInsights.InstrumentationKey}
            };
        }
    }
}
