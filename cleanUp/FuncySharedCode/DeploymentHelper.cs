using System;
using System.IO;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Rest.Azure;
using Newtonsoft.Json.Linq;

namespace FuncySharedCode
{
    public class DeploymentHelper
    {
        private readonly CredentialHelper _credentials;
        private readonly string _deploymentName;
        private readonly string _pathToParameterFile;
        private readonly string _pathToTemplateFile;
        private readonly string _resourceGroupLocation;
        private readonly string _resourceGroupName;

        private ResourceManagementClient _resourceManagementClient;

        public DeploymentHelper(CredentialHelper credentials, string deploymentName,
            string resourceGroupName,
            string resourceGroupLocation)
            : this(
                credentials, deploymentName, resourceGroupName, resourceGroupLocation,
                "Data\\infrastructureParameter.json", "Data\\infrastructureTemplate.json")
        {
        }

        public DeploymentHelper(CredentialHelper credentials, string deploymentName,
            string resourceGroupName,
            string resourceGroupLocation,
            string pathToParameterFile, string pathToTemplateFile)
        {
            this._credentials = credentials;
            this._deploymentName = deploymentName;
            this._resourceGroupName = resourceGroupName;
            this._resourceGroupLocation = resourceGroupLocation;
            this._pathToParameterFile = pathToParameterFile;
            this._pathToTemplateFile = pathToTemplateFile;
        }

        public string StartDeployment()
        {
            var serviceCreds = this._credentials.GetServiceClientCrendtials();

            var template = JObject.Parse(File.ReadAllText(this._pathToTemplateFile));
            var parameters = JObject.Parse(File.ReadAllText(this._pathToParameterFile));

            var rnd = new Random();
            var numberStorageAccounts =
                Convert.ToInt32(parameters["parameters"]["numberStorageAccounts"]["value"].ToString());
            parameters["parameters"]["storageId"]["value"] = rnd.Next(numberStorageAccounts);
            parameters["parameters"]["virtualMachineName"]["value"] = this.GetRandomString();

            this._resourceManagementClient = new ResourceManagementClient(serviceCreds)
            {
                SubscriptionId = this._credentials.SubscriptionId
            };

            this.EnsureResourceGroupExists();
            return this.DeployTemplate(template, parameters);
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
                var resourceGroup = new ResourceGroup {Location = this._resourceGroupLocation};
                this._resourceManagementClient.ResourceGroups.CreateOrUpdate(this._resourceGroupName, resourceGroup);
            }
        }

        /// <summary>
        ///     Starts a template deployment.
        /// </summary>
        /// <param name="templateFileContents">The template file contents.</param>
        /// <param name="parameterFileContents">The parameter file contents.</param>
        private string DeployTemplate(JObject templateFileContents, JObject parameterFileContents)
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
                return "success!!";
            }
            catch (CloudException e)
            {
                return $"Exception Message: {e.Response.Content}";
            }
        }
    }
}