using System;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Amqp
{
    class AmqpLinkFactory : ILinkFactory
    {
        public IAsyncResult BeginOpenLink(AmqpLink link, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return TaskHelpers.ToAsyncResult(this.OpenLinkAsync(link, timeout), callback, state);
        }

        public AmqpLink CreateLink(AmqpSession session, AmqpLinkSettings settings)
        {
            AmqpLink link;
            if (settings.IsReceiver())
            {
                link = new ReceivingAmqpLink(session, settings);
            }
            else
            {
                link = new SendingAmqpLink(session, settings);
            }
            this.OnLinkCreated(link);
            return link;
        }

        public void EndOpenLink(IAsyncResult result)
        {
            TaskHelpers.EndAsyncResult(result);
        }

        public event EventHandler<LinkCreatedEventArgs> LinkCreated;

        protected virtual void OnLinkCreated(AmqpLink o)
        {
            this.LinkCreated?.Invoke(o, new LinkCreatedEventArgs(o));
        }

        public Task OpenLinkAsync(AmqpLink link, TimeSpan timeout)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            if (timeout.TotalMilliseconds > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return TaskConstants.Completed;
        }
    }
}