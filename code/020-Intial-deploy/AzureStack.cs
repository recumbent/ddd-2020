using Pulumi;
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
        var resourceGroup = new ResourceGroup(
            resourceGroupName,
            new ResourceGroupArgs 
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

        // Export the connection string for the storage account
        this.StorageAccountName = storageAccount.Name;
    }

    [Output]
    public Output<string> StorageAccountName { get; set; }
}
