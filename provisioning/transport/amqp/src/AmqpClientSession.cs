using System;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Amqp
{
    public class AmqpClientSession
    {
        readonly AmqpClientConnection amqpConnection;

        public AmqpClientSession(AmqpClientConnection amqpTestConnection)
        {
            this.amqpConnection = amqpTestConnection;
            AmqpSessionSettings = new AmqpSessionSettings();
        }

        private bool _isSessionClosed;

        internal AmqpSession AmqpSession { get; private set; }

        public AmqpSessionSettings AmqpSessionSettings { get; private set; }

        public AmqpClientLink SendingLink { get; private set; }

        public AmqpClientLink ReceivingLink { get; private set; }

        public bool IsSessionClosed => _isSessionClosed;

        public async Task OpenAsync(TimeSpan timeout)
        {
            // Create the Session
            var amqpTestLinkFactory = new AmqpLinkFactory();
            amqpTestLinkFactory.LinkCreated += this.OnLinkCreated;
            this.AmqpSession = new AmqpSession(this.amqpConnection.AmqpConnection, this.AmqpSessionSettings, amqpTestLinkFactory);
            this.amqpConnection.AmqpConnection.AddSession(this.AmqpSession, new ushort?());
            this.AmqpSession.Closed += this.OnSessionClosed;
            await AmqpSession.OpenAsync(timeout).ConfigureAwait(false);
            this._isSessionClosed = false;
        }

        public async Task CloseAsync(TimeSpan timeout)
        {
            var session = this.AmqpSession;
            if (session != null)
            {
                await session.CloseAsync(timeout).ConfigureAwait(false);
            }
        }

        public AmqpClientLink CreateSendingLink(Address address)
        {
            this.SendingLink = CreateLink();
            this.SendingLink.AmqpLinkSettings.SettleType = SettleMode.SettleOnSend;
            this.SendingLink.AmqpLinkSettings.Role = false; // i.e. IsReceiver = false
            this.SendingLink.AmqpLinkSettings.Target = new Target
            {
                Address = address
            };

            return this.SendingLink;
        }

        public AmqpClientLink CreateReceivingLink(Address address)
        {
            this.ReceivingLink = CreateLink();
            this.ReceivingLink.AmqpLinkSettings.SettleType = SettleMode.SettleOnDispose;
            this.ReceivingLink.AmqpLinkSettings.Role = true; // i.e. IsReceiver = true
            this.ReceivingLink.AmqpLinkSettings.Source = new Source
            {
                Address = address
            };

            return this.ReceivingLink;
        }

        public AmqpClientLink CreateLink()
        {
            return new AmqpClientLink(this);
        }

        public event EventHandler<LinkCreatedEventArgs> LinkCreated;

        protected virtual void OnLinkCreated(object sender, LinkCreatedEventArgs args)
        {
            this.LinkCreated?.Invoke(this, new LinkCreatedEventArgs(args.Link));
        }

        void OnSessionClosed(object o, EventArgs args)
        {
            this._isSessionClosed = true;
        }
    }
}