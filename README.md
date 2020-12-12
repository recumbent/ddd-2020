# DDD-20202

DDD 2020 Presentation on Pulumi

This is basically the same presentation I ran for NE-RPC-2020 - there wasn't really enough lead time to re-do the demos so...

## Random helpful things

\\wsl$\Ubuntu-18.04\tmp

docker run -it -v c:\dev\ddd-2020:/repo -p 9000:9000 gitpitch/4.0

[Gitpitch online](https://gitpitch.com/recumbent/ddd-2020/main)

### az snippets

az group list -o table

az resource list --query "[?resourceGroup=='rg-ne-rpc-demo-dev'].{ name: name, flavor: kind, resourceType: type }" --output table

az resource list --query "[?resourceGroup=='rg-ne-rpc-demo-test'].{ name: name, flavor: kind, resourceType: type }" --output table

az storage blob list --container-name data -o table --account-name nerpcdemodev

### Powershell...

Invoke-RestMethod -Method 'Post' -contenttype "application/json" -Body $body0101 -Uri 