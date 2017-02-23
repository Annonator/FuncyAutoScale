using Microsoft.WindowsAzure.Storage.Table;

namespace FuncySharedCode
{
    public class ServerEntity : TableEntity
    {
        public string IP;
        public string Port;
        public string ResourceGroupName;
        public string Status;
        public string VMName;

        public ServerEntity()
        {
        }

        public ServerEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }
    }
}