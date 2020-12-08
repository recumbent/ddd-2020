using Pulumi;
using Pulumi.Azure.AppService;
using Pulumi.Azure.AppService.Inputs;
using Pulumi.Azure.Core;
using Pulumi.Azure.Storage;

class AzureStack : Stack
{
    public AzureStack()
    {
        var config = new Pulumi.Config();
        var deployTo = config.Require("DeployTo");
        
        // Create an Azure Resource Group
        var resourceGroupName = $"rg-ne-rpc-demo-{deployTo}";
        var resourceGroup = new ResourceGroup(resourceGroupName, new ResourceGroupArgs 
        { 
            Name = resourceGroupName 
        });

        // Create an Azure Storage Account
        var storageAccount = new Account($"nerpcdemo{deployTo}", new AccountArgs
        {
            ResourceGroupName = resourceGroup.Name,
            AccountReplicationType = "LRS",
            AccountTier = "Standard"
        });

        // Container for deployment artefacts
        var zipContainer = new Container("zips", new ContainerArgs
        {
            StorageAccountName = storageAccount.Name,
            ContainerAccessType = "private"
        });

        var dataContainer = new Container("data", new ContainerArgs
        {
            StorageAccountName = storageAccount.Name,
            ContainerAccessType = "private",
            Name = "data"
        });

        var appServicePlan = new Plan($"plan-{deployTo}", new PlanArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Kind = "FunctionApp",
            Sku = new PlanSkuArgs
            {
                Tier = "Dynamic",
                Size = "Y1",
            }
        });

        var blob = new Blob("azure-func", new BlobArgs
        {
            StorageAccountName = storageAccount.Name,
            StorageContainerName = zipContainer.Name,
            Type = "Block",
            Source = new FileArchive("../azure-func/publish")
        });

        var codeBlobUrl = 
            SharedAccessSignature.SignedBlobReadUrl(blob, storageAccount);

        var app = new FunctionApp("app", new FunctionAppArgs
        {
            ResourceGroupName = resourceGroup.Name,
            AppServicePlanId = appServicePlan.Id,
            AppSettings =
            {
                { "runtime", "dotnet" },
                { "WEBSITE_RUN_FROM_PACKAGE", codeBlobUrl },
                { "QuoteServerHost", "https://vn651r8t22.execute-api.eu-west-2.amazonaws.com/Prod" },
                { "DataConnectionString", storageAccount.PrimaryBlobConnectionString },
                { "DataContainer", dataContainer.Name }
            },
            StorageAccountName = storageAccount.Name,
            StorageAccountAccessKey = storageAccount.PrimaryAccessKey,
            Version = "~3"
        });

        this.StorageAccountName = storageAccount.Name;
        this.Endpoint = Output.Format($"https://{app.DefaultHostname}/api/lookup");
        this.DataContainer = dataContainer.Name;
    }

    [Output]
    public Output<string> StorageAccountName { get; set; }
    [Output]
    public Output<string> Endpoint { get; set; }
    [Output]
    public Output<string> DataContainer { get; set; }
}
