#r "Microsoft.WindowsAzure.Storage"

using System;
using System.IO;
using System.Linq;
using Microsoft.Azure;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using Microsoft.Rest.Azure.Authentication;
using Newtonsoft.Json.Linq;


public static void Run(TimerInfo myTimer, IQueryable<ServerEntity> inputTable, ICollector<ServerEntity> outputTable, TraceWriter log)
{
    string spId = System.Environment.GetEnvironmentVariable("ServicePrincipleId");
    string spSecret = System.Environment.GetEnvironmentVariable("ServicePrincipleSecret");
    string subscriptionId = System.Environment.GetEnvironmentVariable("SubscriptionId");
    string tenantId = System.Environment.GetEnvironmentVariable("Tenant");

    var credential= new CredentialHelper(spId, spSecret, subscriptionId, tenantId);

    string deployName = DateTime.Now.Day.ToString() + DateTime.Now.Hour.ToString() + DateTime.Now.Minute.ToString();
    var resourceGroupName = "nase";
    var resourceGroupLocation = "westeurope";

    var deploy = new DeploymentHelper(log, credential, deployName, resourceGroupName, resourceGroupLocation);//Name, RGName, RgLocation;

    deploy.StartDeployment();

    var vmName = deploy.vmName;

    var server = new ServerEntity();
    outputTable.Add(
            new ServerEntity(resourceGroupLocation, vmName)
            {
                VMName = vmName,
                IP = "missing",
                Port = "270105",
                ResourceGroupName = resourceGroupName,
                Status = "deploying"
            }
        );

}

public class CredentialHelper
{
    /// <summary>
    ///     The Service Principle Client Id, In the Azure Portal it is called Application Id
    /// </summary>
    private readonly string _clientId;

    private readonly string _clientSecret;

    /// <summary>
    ///     The tenant Id can be accessed through azure CLI with az account show --subscription=ID
    /// </summary>
    private readonly string _tenantId;

    /// <summary>
    ///     Creates a new Crendeital Helper to use ResourceManager APIs and Azure SDKs
    /// </summary>
    /// <param name="servicePrincipleId">In the Azure Portal it is called Application Id</param>
    /// <param name="servicePrincipleSecret">The Secret for your Service Principle</param>
    /// <param name="subscriptionId">The Subscription you want to access with the Service Principle</param>
    /// <param name="tenantId">The tenant Id can be accessed through azure CLI with az account show --subscription=ID</param>
    public CredentialHelper(string servicePrincipleId, string servicePrincipleSecret, string subscriptionId,
        string tenantId)
    {
        this._clientId = servicePrincipleId;
        this._clientSecret = servicePrincipleSecret;
        this.SubscriptionId = subscriptionId;
        this._tenantId = tenantId;
    }

    public string SubscriptionId { get; }


    public ServiceClientCredentials GetServiceClientCrendtials()
    {
        return ApplicationTokenProvider.LoginSilentAsync(this._tenantId, this._clientId, this._clientSecret).Result;
    }

    public SubscriptionCloudCredentials GetSubscriptionCloudCloudCredentials()
    {
        return new TokenCloudCredentials(this.SubscriptionId,
            this.GetSubscriptionCloudCloudCredentials().ToString());
    }
}
public class DeploymentHelper
{
    private readonly CredentialHelper _credentials;
    private readonly string _deploymentName;
    private readonly string _pathToParameterFile;
    private readonly string _pathToTemplateFile;
    private readonly string _resourceGroupLocation;
    private readonly string _resourceGroupName;

    private ResourceManagementClient _resourceManagementClient;

    private static TraceWriter log;

    public string vmName { get; set; }

    public DeploymentHelper(TraceWriter log, CredentialHelper credentials, string deploymentName,
        string resourceGroupName,
        string resourceGroupLocation)
        : this(log,
            credentials, deploymentName, resourceGroupName, resourceGroupLocation,
            "D:\\home\\site\\wwwroot\\AutoVmScale\\Data\\infrastructureParameters.json", "D:\\home\\site\\wwwroot\\AutoVmScale\\Data\\infrastructureTemplate.json")
    {
    }

    public DeploymentHelper(TraceWriter logger, CredentialHelper credentials, string deploymentName,
        string resourceGroupName,
        string resourceGroupLocation,
        string pathToParameterFile, string pathToTemplateFile)
    {
        log = logger;
        this._credentials = credentials;
        this._deploymentName = deploymentName;
        this._resourceGroupName = resourceGroupName;
        this._resourceGroupLocation = resourceGroupLocation;
        this._pathToParameterFile = pathToParameterFile;
        this._pathToTemplateFile = pathToTemplateFile;
    }

    public void StartDeployment()
    {
        var serviceCreds = this._credentials.GetServiceClientCrendtials();

        var template = JObject.Parse(File.ReadAllText(this._pathToTemplateFile));
        var parameters = JObject.Parse(File.ReadAllText(this._pathToParameterFile));

        var rnd = new Random();
        var numberStorageAccounts =
            Convert.ToInt32(parameters["parameters"]["numberStorageAccounts"]["value"].ToString());
        parameters["parameters"]["storageId"]["value"] = rnd.Next(numberStorageAccounts);
        this.vmName = this.GetRandomString();
        parameters["parameters"]["virtualMachineName"]["value"] = this.vmName;

        this._resourceManagementClient = new ResourceManagementClient(serviceCreds)
        {
            SubscriptionId = this._credentials.SubscriptionId
        };

        this.EnsureResourceGroupExists();

        this.DeployTemplate(template, parameters);

    }

    private string GetRandomString()
    {
        var chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var stringChars = new char[5];
        var random = new Random();

        for (var i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }

        return new string(stringChars);
    }

    /// <summary>
    ///     Ensures that a resource group with the specified name exists. If it does not, will attempt to create one.
    /// </summary>
    private void EnsureResourceGroupExists()
    {
        if (this._resourceManagementClient.ResourceGroups.CheckExistence(this._resourceGroupName) != true)
        {
            log.Info($"Creating resource group '{this._resourceGroupName}' in location '{this._resourceGroupLocation}'");
            var resourceGroup = new ResourceGroup { Location = this._resourceGroupLocation };
            this._resourceManagementClient.ResourceGroups.CreateOrUpdate(this._resourceGroupName, resourceGroup);
        }
        else
        {
            log.Info($"Existing resource group '{this._resourceGroupName}' in location '{this._resourceGroupLocation}'");

        }
    }

    /// <summary>
    ///     Starts a template deployment.
    /// </summary>
    /// <param name="templateFileContents">The template file contents.</param>
    /// <param name="parameterFileContents">The parameter file contents.</param>
    private bool DeployTemplate(JObject templateFileContents, JObject parameterFileContents)
    {
        var deployment = new Deployment
        {
            Properties = new DeploymentProperties
            {
                Mode = DeploymentMode.Incremental,
                Template = templateFileContents,
                Parameters = parameterFileContents["parameters"].ToObject<JObject>()
            }
        };


        try
        {
            this._resourceManagementClient.Deployments.BeginCreateOrUpdate(this._resourceGroupName,
                this._deploymentName, deployment);
            return true;
        }
        catch (CloudException e)
        {
            return false;
        }
    }
}
public class ServerEntity : TableEntity
{
    public string IP { get; set; }
    public string Port { get; set; }
    public string ResourceGroupName { get; set; }
    public string Status { get; set; }
    public string VMName { get; set; }

    public ServerEntity()
    {
    }

    public ServerEntity(string partitionKey, string rowKey)
    {
        this.PartitionKey = partitionKey;
        this.RowKey = rowKey;
    }
}
