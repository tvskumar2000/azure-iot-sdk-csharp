using System;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Amqp
{
    public class LinkCreatedEventArgs : EventArgs
    {
        public LinkCreatedEventArgs(AmqpLink link)
        {
            this.Link = link;
        }

        public AmqpLink Link { get; }
    }
}