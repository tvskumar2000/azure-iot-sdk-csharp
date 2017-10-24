using System;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.Azure.Amqp.Framing;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Amqp
{
    public class AmqpClientLink
    {
        readonly AmqpClientSession amqpSession;

        public AmqpClientLink(AmqpClientSession amqpClientSession)
        {
            this.amqpSession = amqpClientSession;

            this.AmqpLinkSettings = new AmqpLinkSettings
            {
                LinkName = "DeviceLink_" + Guid.NewGuid().ToString().Substring(0, 8),
                TotalLinkCredit = AmqpConstants.DefaultLinkCredit,
                AutoSendFlow = true,
                Source = new Source(),
                Target = new Target(),
                SettleType = SettleMode.SettleOnDispose
            };
        }

        private bool _isLinkClosed;

        internal AmqpLink AmqpLink { get; private set; }

        public AmqpLinkSettings AmqpLinkSettings { get; private set; }

        public bool IsLinkClosed => _isLinkClosed;

        public async Task OpenAsync(TimeSpan timeout)
        {
            if (Extensions.IsReceiver(this.AmqpLinkSettings))
            {
                this.AmqpLink = new ReceivingAmqpLink(this.amqpSession.AmqpSession, this.AmqpLinkSettings);
            }
            else
            {
                this.AmqpLink = new SendingAmqpLink(this.amqpSession.AmqpSession, this.AmqpLinkSettings);
            }

            AmqpLink.SafeAddClosed(this.OnLinkClosed);
            await AmqpLink.OpenAsync(timeout).ConfigureAwait(false);
            _isLinkClosed = false;
        }

        void AddProperty(AmqpSymbol symbol, object value)
        {
            Extensions.AddProperty((Attach)this.AmqpLinkSettings, symbol, value);
        }

        public void AddApiVersion(string apiVersion)
        {
            this.AddProperty(AmqpConstants.Vendor + ":api-version", apiVersion);
        }

        public void AddClientVersion(string clientVersion)
        {
            this.AddProperty(AmqpConstants.Vendor + ":client-version", clientVersion);
        }

        public async Task<Outcome> SendMessageAsync(
            AmqpMessage message,
            ArraySegment<byte> deliveryTag,
            TimeSpan timeout)
        {
            var sendLink = AmqpLink as SendingAmqpLink;
            if (sendLink == null)
            {
                throw new InvalidOperationException("Link does not support sending.");
            }

            return await sendLink.SendMessageAsync(message,
                deliveryTag,
                AmqpConstants.NullBinary,
                timeout).ConfigureAwait(false);
        }

        public async Task<AmqpMessage> ReceiveMessageAsync(TimeSpan timeout)
        {
            var receiveLink = AmqpLink as ReceivingAmqpLink;
            if (receiveLink == null)
            {
                throw new InvalidOperationException("Link does not support receiving.");
            }

            return await receiveLink.ReceiveMessageAsync(timeout).ConfigureAwait(false);
        }

        public void AcceptMessage(AmqpMessage amqpMessage)
        {
            var receiveLink = this.AmqpLink as ReceivingAmqpLink;
            if (receiveLink == null)
            {
                throw new InvalidOperationException("Link does not support receiving.");
            }
            receiveLink.AcceptMessage(amqpMessage, false);
        }

        void OnLinkClosed(object o, EventArgs args)
        {
            _isLinkClosed = true;
        }
    }
}