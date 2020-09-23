module Program

open Pulumi
open Pulumi.FSharp
open Pulumi.Azure.Authorization
open Pulumi.Azure.Core
open Pulumi.Azure.Dns
open Pulumi.Azure.KeyVault
open Pulumi.Azure.Storage
open Pulumi.AzureAD

// These are the "mailNickname" values for users as we currently have them defined in AzureAD, being the least complex option
let users =
    [
        "tom@example.com"
        "dick@example.com"
        "harriet@example.com"
        "jamie@example.com"
    ]

let makeRecord (resourceGroup : ResourceGroup) (zone : Zone) (x, y, z) =
     let args = CNameRecordArgs (ResourceGroupName = (io resourceGroup.Name), Ttl = input 300, ZoneName = (io zone.Name), Name = input y, Record = input z )
     let cname = CNameRecord (x, args)
     ()

let sendGridDomainValidation resourceGroup zone =
    let data = 
        [ 
            ("sendgrid-xxxxx", "xxxxx", "u1YYYYYYYY.wlzzz.sendgrid.net")
            ("sendgrid-s1", "s1._domainkey", "s1.domainkey.uYYYYYYYY.wlzzz.sendgrid.net")
            ("sendgrid-s2", "s2._domainkey", "s2.domainkey.uYYYYYYYY.wlzzz.sendgrid.net")
        ]

    data |> Seq.iter (makeRecord resourceGroup zone)

let dnsInfra () = 

    // Create an Azure Resource Group
    let dnsResourceGroup = ResourceGroup "development-dns"

    let zoneArgs = 
        ZoneArgs  (Name = input "totallyfake.test", ResourceGroupName = (io dnsResourceGroup.Name))

    let zone = Zone ("totallyfake", zoneArgs)

    sendGridDomainValidation dnsResourceGroup zone

    [
        ("nameServers", zone.NameServers :> obj)
    ]

let pulumiInfra () =
    // -------------------------
    // Infrastructure for Pulumi
    // -------------------------

    let current = Output.Create<Azure.Core.GetClientConfigResult>(Azure.Core.GetClientConfig.InvokeAsync())

    let subscriptionId = "6ef0069f-b712-498b-b6bf-0b9a0423dd87"

    let getUsersArgs = GetUsersArgs(MailNicknames = ResizeArray(users))
    let usersResult = GetUsers.InvokeAsync(getUsersArgs) |> Async.AwaitTask |> Async.RunSynchronously

    let members = 
        usersResult.ObjectIds 
        |> Seq.map input 

    let devGroup = 
        Group
            (
                "pulumi-developers",
                GroupArgs
                    (   
                        Description = input "Users that do pulumi development",
                        Members = inputList members
                    )
            )

    // Service principal for deployment, starts with an application, may need >1 ?
    let pulumiDeployApp = 
        Application(
            "pulumiDeployApp",
            ApplicationArgs(
                Homepage = input "http://pulumiDeploy",
                IdentifierUris = inputList [ input "http://pulumiDeploy" ],
                AvailableToOtherTenants = input false,
                Oauth2AllowImplicitFlow = input true
                )
            )    

    let pulumiDeployServicePrincipal =
        ServicePrincipal(
            "pulumiDeployServicePrincipal",
            ServicePrincipalArgs(
                ApplicationId = io pulumiDeployApp.ApplicationId,
                AppRoleAssignmentRequired = input false
            )
        )

    // Assign Creator role to SP - this is for all of the subscription
    let subscriptionScope = sprintf "/subscriptions/%s" subscriptionId
    let spRoleAssignment = 
        Assignment(
            "pulumiDeployCreatorAssignment",
            AssignmentArgs(
                Scope = input subscriptionScope,
                RoleDefinitionName = input "Contributor",
                PrincipalId = io pulumiDeployServicePrincipal.ObjectId
            )
        )

    // Create an Azure Resource Group
    let pulumiResourceGroupName = "pulumi-infrastructure"
    let pulumiResourceGroup = ResourceGroup 
                                (pulumiResourceGroupName,
                                 ResourceGroupArgs
                                    (Name = input "pulumi-infrastructure",
                                     Location = input "UKSouth"
                                    )
                                )

    // Create an Azure Storage Account for pulumi back end data
    let storageAccount =
        Account("pulumi-storage",
            AccountArgs
               (ResourceGroupName = io pulumiResourceGroup.Name,
                AccountReplicationType = input "LRS",
                AccountTier = input "Standard",
                Name =  input "examplepulumistorage"
               ))

    // Create a container for the platform stacks
    let container =
        Container("platform",
            ContainerArgs
                (StorageAccountName = io storageAccount.Name,
                 ContainerAccessType = input "private",
                 Name = input "platform"
                ))

    // Need [ input one; input two; input three; ... ]
    let keyPermissions =
        [ "list"; "create"; "get"; "update"; "encrypt"; "decrypt" ], // Notably no delete permission, which is right, but fun...
        |> List.map input

    let secretPermissions =
        [ "delete"; "get"; "list"; "set" ]
        |> List.map input
    
    // Define policy for key vault
    let vaultPolicy = Inputs.KeyVaultAccessPolicyArgs
                        (TenantId = io (current.Apply(fun c -> c.TenantId)),
                         ObjectId = io devGroup.ObjectId,
                         KeyPermissions = inputList keyPermissions
                         SecretPermissions = inputList secretPermissions
                        )

    let spVaultPolicy =
         Inputs.KeyVaultAccessPolicyArgs
             (TenantId = io (current.Apply(fun c -> c.TenantId)),
              ObjectId = io pulumiDeployServicePrincipal.ObjectId,
              KeyPermissions = inputList KeyPermissions
             )

    // Create a key vault
    let keyVault =
        KeyVault("example-pulumi-vault",
            KeyVaultArgs
                (Name = input "example-pulumi-vault",
                 ResourceGroupName = io pulumiResourceGroup.Name,
                 SkuName = input "standard",
                 TenantId = io (current.Apply(fun c -> c.TenantId)),
                 SoftDeleteEnabled = input true,
                 AccessPolicies = inputList [ input vaultPolicy; input spVaultPolicy ]
                ))

    // Create a key in the vault for encrypting pulumi platform stack secrets
    let key =
        Key("platform",
            KeyArgs
                (KeyVaultId = io keyVault.Id,
                 KeyType = input "RSA",
                 KeySize = input 2048,
                 KeyOpts = inputList [ input "encrypt"; input "decrypt" ],
                 Name = input "platform"
                ))

    [
        ("connectionString",   storageAccount.PrimaryConnectionString :> obj)
        ("containerName",      container.Name :> obj)
        ("developerGroupName", devGroup.Name :> obj)
        ("keyVaultName",       keyVault.Name :> obj)
        ("keyVaultUri",        keyVault.VaultUri :> obj)
        ("platformKeyName",    key.Name :> obj)
        ("deployServicePrincipal.DisplayName", pulumiDeployServicePrincipal.DisplayName :> obj)
        ("deployServicePrincipal.ApplicationId", pulumiDeployServicePrincipal.ApplicationId :> obj)
        ("deployServicePrincipal.Id", pulumiDeployServicePrincipal.Id :> obj)
        ("deployServicePrincipal.ObjectId", pulumiDeployServicePrincipal.ObjectId :> obj)
    ]

let infra () =
    dnsInfra() @
    pulumiInfra()
    |> dict


[<EntryPoint>]
let main _ =
  Deployment.run infra