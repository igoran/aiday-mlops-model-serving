using System;
using System.Text.RegularExpressions;
using infra;
using Pulumi;
using Pulumi.Azure.AppInsights;
using Pulumi.Azure.AppService;
using Pulumi.Azure.AppService.Inputs;
using Pulumi.Azure.Core;
using Pulumi.Azure.Storage;

class MyStack : Stack
{
    public string ProjectStack { get; }
    public string StackSuffix { get; }

    public string GetModelVersion()
    {
        if (Deployment.Instance.IsDryRun)
        {
            return "https://localhost/model.zip";
        }

        var modelVersion = System.Environment.GetEnvironmentVariable("ML_MODEL_URI");

        if (string.IsNullOrEmpty(modelVersion))
        {
            throw new ArgumentNullException("ML_MODEL_URI","Null or empty Model Version");
        }

        return modelVersion;
    }

    public MyStack()
    {
        ProjectStack = $"{Deployment.Instance.ProjectName}-{Deployment.Instance.StackName}";

        StackSuffix = Regex.Replace(Deployment.Instance.StackName, "[^a-z0-9]", string.Empty, RegexOptions.IgnoreCase);

        var modelVersion = GetModelVersion();

        Console.WriteLine($"ML Model Version {modelVersion}");

        var resourceGroup = new ResourceGroup(ProjectStack);

        var storageAccount = new Account("sa" + StackSuffix.ToLowerInvariant(), new AccountArgs
        {
            ResourceGroupName = resourceGroup.Name,
            AccountReplicationType = "LRS",
            AccountTier = "Standard"
        });

        var appServicePlan = new Plan("asp" + StackSuffix.ToLowerInvariant(), new PlanArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Kind = "FunctionApp",
            Sku = new PlanSkuArgs
            {
                Tier = "Dynamic",
                Size = "Y1",
            }
        });

        var container = new Container("cntzip" + StackSuffix.ToLowerInvariant(), new ContainerArgs
        {
            StorageAccountName = storageAccount.Name,
            ContainerAccessType = "private"
        });

        var blob = new Blob("blobzip" + StackSuffix.ToLowerInvariant(), new BlobArgs
        {
            StorageAccountName = storageAccount.Name,
            StorageContainerName = container.Name,
            Type = "Block",
            Source = new FileArchive("../ml/Predictor/bin/Release/netcoreapp3.1/publish/")
        });

        var codeBlobUrl = SharedAccessSignature.SignedBlobReadUrl(blob, storageAccount);

        var appInsights = new Insights("fxai" + StackSuffix.ToLowerInvariant(), new InsightsArgs
        {
            ResourceGroupName = resourceGroup.Name,
            ApplicationType = "web"
        });

        var appSettings = new InputMap<string>()
        {
                {"runtime", "dotnet"},
                {"WEBSITE_RUN_FROM_PACKAGE", codeBlobUrl},
                {"AzureWebJobsStorage", storageAccount.PrimaryConnectionString},
                {"ML_MODEL_URI", modelVersion},
                {"APPINSIGHTS_INSTRUMENTATIONKEY", appInsights.InstrumentationKey}
        };

        var appFx = new MyFunctionApp("fxapp" + StackSuffix.ToLowerInvariant(), resourceGroup, appServicePlan, storageAccount, appSettings);

        StorageConnectionString = Output.Format($"{storageAccount.PrimaryConnectionString}");

        Endpoint = Output.Format($"https://{appFx.DefaultHostname}");
    }

    [Output]
    public Output<string> StorageConnectionString { get; set; }
    [Output]
    public Output<string> Endpoint { get; set; }
}