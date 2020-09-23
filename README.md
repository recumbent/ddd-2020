# ne-rpc-2020

NE RPC 2020 Back to School Presentation

## Random helpful things

\\wsl$\Ubuntu-18.04\tmp

docker run -it -v c:\dev\ne-rpc-2020:/repo -p 9000:9000 gitpitch/desktop:pro

[Gitpitch online](https://gitpitch.com/recumbent/ne-rpc-2020)

### az snippets

az group list

az resource list --query "[?resourceGroup=='rg-ne-rpc-demo-dev'].{ name: name, flavor: kind, resourceType: type }" --output table

az resource list --query "[?resourceGroup=='rg-ne-rpc-demo-test'].{ name: name, flavor: kind, resourceType: type }" --output table
