using Pulumi;
using Pulumi.Aws.Iam;
using Pulumi.Aws.Lambda;
using Pulumi.Aws.Lambda.Inputs;
using Pulumi.Aws.S3;

class AwsStack : Stack
{
    public AwsStack()
    {
        var config = new Pulumi.Config();
        var deployTo = config.Require("DeployTo");

        var tags = new InputMap<string>
            {
                { "User:Project",     Pulumi.Deployment.Instance.ProjectName },
                { "User:Stack",       Pulumi.Deployment.Instance.StackName }
            };

        // Create an AWS resource (S3 Bucket)
        var dataBucket = new Bucket(
            $"ne-rpc-demo-data-{deployTo}",
            new BucketArgs
            {
                Tags = tags
            }
        );

        // Create a role to run the lambda
        var lambdaRole = new Role("lambdaRole", new RoleArgs
        {
            AssumeRolePolicy =
            @"{
                    ""Version"": ""2012-10-17"",
                    ""Statement"": [
                        {
                            ""Action"": ""sts:AssumeRole"",
                            ""Principal"": {
                                ""Service"": ""lambda.amazonaws.com""
                            },
                            ""Effect"": ""Allow"",
                            ""Sid"": """"
                        }
                    ]
                }"
            }
        );

        var logPolicy = new RolePolicy("lambdaLogPolicy", new RolePolicyArgs
        {
            Role = lambdaRole.Id,
            Policy =
        @"{
                ""Version"": ""2012-10-17"",
                ""Statement"": [{
                    ""Effect"": ""Allow"",
                    ""Action"": [
                        ""logs:CreateLogGroup"",
                        ""logs:CreateLogStream"",
                        ""logs:PutLogEvents""
                    ],
                    ""Resource"": ""arn:aws:logs:*:*:*""
                }]
            }"
        });
        
        var bucketPolicy = new RolePolicy("lambdaBucketPolicy", new RolePolicyArgs
        {
            Role = lambdaRole.Id,
            Policy =
        Output.Format($@"{{
            ""Version"": ""2012-10-17"",
            ""Statement"": [
                {{
                    ""Effect"": ""Allow"",
                    ""Action"": [
                        ""s3:*""
                    ],
                    ""Resource"": ""{dataBucket.Arn}""
                }},
                {{
                    ""Effect"": ""Allow"",
                    ""Action"": [
                        ""s3:*""
                    ],
                    ""Resource"": ""{dataBucket.Arn}/*""
                }}
            ]
        }}")
        });

        var quoteFetch = new Function(
            "quote-fetch-lambda", 
            new FunctionArgs
            {
                Runtime = "dotnetcore3.1",
                Code = new FileArchive("../aws-lambda/publish"),
                Handler = "aws-lambda::Recumbent.Demo.Aws.AwsLambda::FunctionHandler",
                Environment = new FunctionEnvironmentArgs
                {
                    Variables = new InputMap<string>
                    {
                        { "QuoteServerHost", "https://vn651r8t22.execute-api.eu-west-2.amazonaws.com/Prod" },
                        { "DataBucket", dataBucket.Id },
                    }              
                },
                Role = lambdaRole.Arn,
                Timeout = 60,
                Tags = tags
            }
        );


        // Export useful things
        this.DataBucketName = dataBucket.Id;
        this.DataBucketArn = dataBucket.Arn;
        this.FunctionInvokeArn = quoteFetch.InvokeArn;
    }

    [Output]
    public Output<string> DataBucketName { get; set; }
    [Output]
    public Output<string> DataBucketArn { get; set; }
    [Output]
    public Output<string> FunctionInvokeArn { get; set; }
}
