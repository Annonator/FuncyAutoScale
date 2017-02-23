#r "Microsoft.WindowsAzure.Storage"
#r "../SharedCode/FuncySharedCode.dll"

using System;
using System.IO;
using System.Linq;
using Microsoft.Azure;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using Microsoft.Rest.Azure.Authentication;
using Newtonsoft.Json.Linq;

using FuncySharedCode;

public static void Run(TimerInfo myTimer, IQueryable<ServerEntity> inputTable, ICollector<ServerEntity> outputTable, TraceWriter log)
{
    string spId = System.Environment.GetEnvironmentVariable("ServicePrincipleId");
    string spSecret = System.Environment.GetEnvironmentVariable("ServicePrincipleSecret");
    string subscriptionId = System.Environment.GetEnvironmentVariable("SubscriptionId");
    string tenantId = System.Environment.GetEnvironmentVariable("Tenant");

    var test = new CredentialHelper(spId, spSecret, subscriptionId, tenantId);
}
