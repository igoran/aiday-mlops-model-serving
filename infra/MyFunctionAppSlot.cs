using Pulumi;
using Pulumi.Azure.AppService;
using Pulumi.Azure.AppService.Inputs;

namespace infra
{
    public class MyFunctionAppSlot : FunctionAppSlot
    {
        public MyFunctionAppSlot(string slotName, FunctionAppSlot app, InputMap<string> settings) : base(slotName, new FunctionAppSlotArgs()
        {
            Name = slotName,
            ResourceGroupName = app.ResourceGroupName,
            AppServicePlanId = app.AppServicePlanId,
            StorageAccountName = app.StorageAccountName,
            StorageAccountAccessKey = app.StorageAccountAccessKey,
            FunctionAppName = app.Name,
            Version = "~3",
            AppSettings = settings,
            SiteConfig = new FunctionAppSlotSiteConfigArgs
            {
                Cors = new FunctionAppSlotSiteConfigCorsArgs
                {
                    AllowedOrigins = new InputList<string>
                        {
                            "http://localhost:5500"
                        },
                    SupportCredentials = true
                }
            }
        })
        {
            if (string.IsNullOrWhiteSpace(slotName))
            {
                throw new System.ArgumentException($"'{nameof(slotName)}' cannot be null or whitespace", nameof(slotName));
            }
        }
    }
}
