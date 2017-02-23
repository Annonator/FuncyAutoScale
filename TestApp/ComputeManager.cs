using System.Linq;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Network;

namespace TestApp
{
    public class ComputeManager
    {
        private readonly CredentialHelper _credentials;

        public ComputeManager(CredentialHelper credentials)
        {
            this._credentials = credentials;
        }

        public void Deallocate(string resourceGroupName, string vmName)
        {
            var computeManagementClient = new ComputeManagementClient(this._credentials.GetServiceClientCrendtials());
            computeManagementClient.VirtualMachines.BeginDeallocateWithHttpMessagesAsync(resourceGroupName, vmName);
            //computeManagementClient.VirtualMachines.BeginDeallocating(resourceGroupName, vmName);
        }

        public void Allocate(string resourceGroupName, string vmName)
        {
            var computeManagmentClient = new ComputeManagementClient(this._credentials.GetServiceClientCrendtials());
            computeManagmentClient.VirtualMachines.BeginStartWithHttpMessagesAsync(resourceGroupName, vmName);
            //computeManagmentClient.VirtualMachines.BeginStarting(resourceGroupName, vmName);
        }

        public string GetPublicIpFromVm(string resourceGroupName, string vmName)
        {
            var client = new ComputeManagementClient(this._credentials.GetServiceClientCrendtials())
            {
                SubscriptionId = this._credentials.SubscriptionId
            };

            var vm = client.VirtualMachines.Get(resourceGroupName, vmName);

            var networkName = vm.NetworkProfile.NetworkInterfaces[0].Id.Split('/').Last();

            var networkClient = new NetworkManagementClient(this._credentials.GetServiceClientCrendtials())
            {
                SubscriptionId = this._credentials.SubscriptionId
            };

            var ipResourceName =
                networkClient.NetworkInterfaces.Get(resourceGroupName, networkName).IpConfigurations[0].PublicIPAddress
                    .Id.Split('/').Last();

            return networkClient.PublicIPAddresses.Get(resourceGroupName, ipResourceName).IpAddress;
        }

        public bool IsDeploymentSucceded(string resourceGroupName, string vmName)
        {
            var client = new ComputeManagementClient(this._credentials.GetServiceClientCrendtials())
            {
                SubscriptionId = this._credentials.SubscriptionId
            };

            var vm = client.VirtualMachines.Get(resourceGroupName, vmName);

            return vm.ProvisioningState.Equals("Succeeded");
        }
    }
}