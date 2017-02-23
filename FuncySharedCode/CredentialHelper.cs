using Microsoft.Azure;
using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;

namespace FuncySharedCode
{
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
}