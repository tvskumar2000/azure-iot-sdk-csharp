using System.Net;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Amqp
{
    public class TpmNetworkCredential : NetworkCredential
    {
        public TpmNetworkCredential(string endorsementKey, string storageRootKey)
        {
            this.EndorsementKey = endorsementKey;
            this.StorageRootKey = storageRootKey;
        }

        public string EndorsementKey { get; private set; }
        public string StorageRootKey { get; private set; }
    }
}