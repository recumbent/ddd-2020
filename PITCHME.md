# Infrastructure as (.NET) code in Pulumi 

---

# Cloud Software Engineer

Note:

Hi, my name is James Murphy, my job title is cloud software engineer, I first laid hands to keyboard in 1979 and I've been writing code and learning new things ever since

---

# Pulumi

* www.pulumi.com

Note:

Pulumi is a tool for defining infrastructure as code - genuine source code, its also a tool and a service for managing deployment of that infrastructure

---

Questions:

* What is infrastructure as code?
* Why is it a good idea?
* If it _is_ a good idea, why Pulumi?

Note:

That definition immediately raises a few questions - what is infrastructre as code? why is it a good idea? and if it is a good idea then why Pulumi?

---

# What do we do?

Note:

Let me ask a different question - what is it that we do? At the most basic level we find problems and deliver solutions to those problems - in my case those solutions are deployed to (and take advantages of the services provided by) a public cloud (be that Azure or AWS or GCP or...).

So we have a definition of delivering value that says we need a deployed solution available to our users.

---

# Effective?

Note:

To be effective in delivering we can take guidance from the experience of others - in Accelerate by Dr Nicole Forzgren et al we have clear evidence that the time to go from committed code to deployed application correlates very strongly with high performing teams. This leads us to the principals in Continuous Delivery by Jez Humble and Dave Farley that we build and package _once_ and deploy to multiple environments as needed but that we always deploy in the same way whether it be to the dev environment or to staging or to production. And that we automate. Everything. We automate things to make them trivially repeatable and that has to include deployment.

---

# Automate! Everything!

Note:

So how do we automate definition of infrastructure or the definition of deployed systems (where there is considerable overlap).

---

# Scripting?

Note: 

There are a couple of approaches - we can "script" things - all of the cloud providers provide rest APIs and command line tools to create and configure services - unfortunately we want our definitions to be idempotent, we want to safely be able to run the script whether the services exist or not and that gets difficult quickly.

---

# Desired State

* Make it so!
  
Note:

Better is to take the Picard approach, to define what our infrastructure should look like and then tell someone else to "Make it so" - this is "desired state configuration".

The good news is that the public cloud providers support this approach - they have declarative systems for specifying your requirements and engines that magically transforms those specifications into cloud infrastructure. The bad news is that we very quickly end up with a "wall of markup" - be that json or yaml 

---

# Insert Azure ARM here

Note:

This is the azure ARM template for my sample system (X lines long). The equivalent in AWS Cloudformation YAML will be as large

The problem with these templates is that they are hard to read and therefore hard to maintain - there is a huge amount of duplicated boilerplate and not a small amount or arcane cleverness where programming concepts are added to markup to overcome the limitiations of straight markup. So the templates require you to understand not only the infrastructure you're targetting but also effectively a whole new language used to create and configure that infrastructure.

---

# Team Topologies

Note:

Coming back to our core problem - delivering value by making deployed solutions available - there is a strong argument, outlined in the excellent Team Topologies, that delivery teams (5 to 9 people) should own solutions end to end, if you write it then you should deploy it and you should operate it.

# Cognotive Load

The thinker

Note:

The limiting factor on what a team can own and operate is how much cognative load it can support, what it can comfortably think about. Having to understand ARM or Cloudformation or the YAML for Google's Cloud Deployment Manager will consume a goodly chunk of that available load.

So we want something that is both more flexible than XML or YAML and that is more easily understood by the developers on a delivery team - in other words source code.

---

# Code...

* AWS CDK
* Azure - [Farmer](https://compositionalit.github.io/farmer/), [Bicep](https://github.com/Azure/bicep)
* Pulumi
  * Python, Go, Typescript, .NET
  
Note:

- For AWS amazon are providing the CDK
- For Azure there is the very impressive Farmer (its an F# DSL, I'm bound to like it...) and Microsoft are experimenting with Bicep
- Or there is Pulumi, which targets multiple services and which lets you write code in Python, Go, and in languages that support Node.js, and .NET. - and that means it probable that your team can define the system it wants to deploy using something it already understands and removes some of the additional load leaving more to think about the actual problem space rather than how to shave yaks.

The other advantage we gain from having infrastructure defined in text files (whatever the format) is that we can store that infrastructure with our application source code - that we can see it evolve and can audit it in the same way we do all our source code (and we can revert changes in much the same way)

---

- Repeatable
- Deploy to multiple environments
- Deploy at whim
- Version controlled

Note: 

At this point its clear that defining our infrastructure in a text file is a good thing

- It makes things repeatable
- It makes it possible to deploy to as many environments as we want as and when we want
- We can use version control which gives us a history and the ability to audit and control what we do.

---

## Pulumi

* Language independence
* Platform independence
* Deployment management

Note:

On top of this Pulumi gives us

- A degree of language independence
- A degree of target platform independence
- A service/system for managing deployments

That's probably enough why - although I could easily fill the rest of my time, but I promised some code and I think that seeing code is important

So time for some how.

---

### Architecture Diagram

Note:

Suppose we have an application something like this - a service that retrieves some data, transforms it, caches it in a data store and returns the result.

If I were building this for work my first steps would be to deploy a "hello world" version through to production, so that's what we'll do. In the real world that would involve setting up my CI pipeline too - but that's a different presentation.

---

### Getting started

Note:

Pulumi operates from the command line - so we install by appropriate means for your dev platform.

Once installed

---?terminal=sessions/pulumi-new.cast&font=16px&theme=monokai&poster=npt:0:00&color=#DDDDDD

Note:

`> pulumi version`

When we use pulumi it needs to store state - to be able to keep track of what is deployed where and what things are called. By default it expects you to use their service - not unreasonably - but as that is not free, you can also store data locally or in cloud storage. Regardless we need to login to a backend, I'll use local storage for the demos but I'd strongly advocate looking at Pulumi's paid services (I'm advocating for that at work)

So we can do pulumi login cloud azureblob etc

Then, we create a new folder for our project and we run the init command - when we do we can specify a template, we're targetting azure and want to write C# code so... 

md pulumi
cd pulumi

pulumi init azure-csharp

...talk over prompts...

And we end up with a standard C# project - something that looks like a console app.

---

### New project

Note:

What's in this? Lets take a look.

The entry point is in program.cs, at this point it does very little, it instantiates a class and then calls it

The more interesting part of the story is in the class itself - this is where we define the resources we want to exist.

Going top down, we have a resource group

A storage account - in the resource group (the location is pulled from the config)

Finally we set the connection string as an output - which is defined as a property on the class.

It tells me that it wants to create three things, first is the stack itself, second is the resource group and third is a storage account.

That's cool, but I actually want more than one stack - for demo purposes lets have dev and test (I'd expect a few more in real life). We've got our dev stack setup so lets add another and then change the code to pick up stack name

...pulumi stack add...

We'll specify the target environment as a config variable - lets call it "deployto" (because environment is so badly overloaded)

Appropriate code to parameterise instantiating the stack

Lets switch back to the dev stack, and then lets go look at my subscription in Azure... what we see is a whole load of nothing.

To create the resources we run pulumi up

...pulumi up...

Now if we go look at azure we see a single resource group and in that resource group the storage account - I also see that those resources have a random suffix to avoid collisions and to make some actions easier and safer. I happen to not like this much, but that's a more complicated conversation so lets stick with the default behaviour.

Lest run `pulumi up` again, this time for for the prod stack, and now we have _two_ resource groups and the second also has a storage account... and we see the name is different to match the target environment (as well as having a different random suffix)

Lets add a function (or a container?), 

The function itself I prepared earlier and I'm running `dotnet publish` to put the deployable content in a known location

If we open up an editor of choice / or "look at the code" - then we need to work through a number of steps to get an azure function live (more than I'd like... but sometimes the portals hide a lot of what's necessary from us)

see https://github.com/pulumi/examples/blob/master/azure-cs-functions-consumption/FunctionsStack.cs

We create:
* An app service plan - this is somewhere for our function to run, the Sku is one of those irritating details you have to "just know"
* A container to hold the deployment package - the zipped function
* The deployment package itself - this is being zipped as part of the build, which is not what we want in the real world but will work here
* And finally the function app itself - which we pass the resource, the app service, a reference to the zip containing the function and details of details of the storage account
* We make the function endpoint an output

If we save everything, we can run pulumi preview and it will show us the changes we want to make

...pulumi preview...

We're adding various new things, but we're not updating anything that already exists.

Lets run that

...pulumi up...

Now my dev stack has a function - and I can invoke that URL and get the result we expect

But if we look at the prod resource group - that's unchanged.

How does pulumi keep track of all this - if we go look at in the folder created for the back end we'll find a _lot_ of json. In an ideal world one would never actually care about the content, but if we were to take a look one would see that the resources in code are also defined in the json - and that pulumi has given everything a unique identifier

When we run pulumi up (or preview) the _desired_ state is determined from the input source and then the engine compares that desired state with the actual resources and, if appropriate, attempts to make the changes necessary to bring the actual resouces in line with that state.

Each time we allow pulumi to make changes it records checkpoints - so we can run pulumi stack history and see the changes we've made.

The real function is intended to take json as a parameter interact with a different  service and return json as a result. It needs its own container for storage and we need to set environment values so that it can find that container and the service it depends on.

I'll updated my function, so lets add the pulumi code to manage that that... I've deployed my quote service as its own stack, so we'll pull the endpoint URL in.

Here we write stuff out so that the app can read it in, and off we go.

That's it working for azure... what about aws?

I have the same core code but wrapped in a lambda function rather than an azure function - and from there the principals are the same.

I've created a new stack and I'm going to to put a little logic in my program to decide which stack to instantiate. 

The aws stack looks like this... note that I'm adding tags to let me group things, strictly in the real world I'd probbaly deploy to different accounts, but this is still useful.

Whilst the types and the details vary the patterns are the same and you've got help.

One last example, from azure, in F# - getting a bit meta.

I needed to create a group in AzureAD to control access to a key vault.

I have a list of users that I want in the group... 

Pulumi lets me look up existing users

Given the list of existing users I can extract their ids and use those when I create a group.

And that's it, I can then use the group to create my vault...

When we need to add or remove users we change that list, run the stack, and we're done.

Over time I hope we'll be able to apply this to all our dev infrastructure - we'll import the exist resources etc into stacks and then build out from there. There are some security concerns, but equally there is a huge upside to working this way - in particular onboarding will be a matter of adding a user to the right list and working from there. But equally adding a new repository with the rules we require should be the same, add the name to the right list, go...

Ok... so that barely touches the surface of what's possible - but this is _all code_ and hopefully code that you understand be that .NET, Python, Go, or Typescript. 

You can self host - or I would strongly urge taking a look at the value propoposition of the hosted service.
