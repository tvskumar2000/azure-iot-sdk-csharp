using System;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Transport;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Amqp
{
    public class AmqpClientConnection
    {
        readonly AmqpSettings amqpSettings;
        readonly Uri uri;

        internal AmqpClientConnection(Uri uri, AmqpSettings amqpSettings)
        {
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
            this.uri = uri;
            this.amqpSettings = amqpSettings;

            this.AmqpConnectionSettings = new AmqpConnectionSettings
            {
                ContainerId = Guid.NewGuid().ToString(),
                HostName = uri.Host,
            };
        }

        public AmqpConnection AmqpConnection { get; private set; }

        public AmqpConnectionSettings AmqpConnectionSettings { get; private set; }

        public TlsTransportSettings TransportSettings { get; private set; }

        public AmqpClientSession AmqpSession { get; private set; }

        public bool IsConnectionClosed => _isConnectionClosed;

        private bool _isConnectionClosed;

        public async Task OpenAsync(TimeSpan timeout, bool useWebSocket, X509Certificate2 clientCert)
        {
            var timeoutHelper = new TimeoutHelper(timeout);
            var hostName = this.uri.Host;

            var tcpSettings = new TcpTransportSettings { Host = hostName, Port = this.uri.Port != -1 ? this.uri.Port : AmqpConstants.DefaultSecurePort };
            this.TransportSettings = new TlsTransportSettings(tcpSettings)
            {
                TargetHost = hostName,
                CertificateValidationCallback = (sender, cert, chain, errors) => true,
                Certificate = clientCert ?? null
            };

            TransportBase transport;

            if (useWebSocket)
            {
                transport = await CreateClientWebSocketTransportAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            }
            else
            {
                var tcpInitiator = new AmqpTransportInitiator(amqpSettings, TransportSettings);
                transport = await tcpInitiator.ConnectTaskAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            }

            this.AmqpConnection = new AmqpConnection(transport, amqpSettings, this.AmqpConnectionSettings);
            await AmqpConnection.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            this._isConnectionClosed = false;
            this.AmqpConnection.Closed += this.OnConnectionClosed;
        }

        public async Task CloseAsync(TimeSpan timeout)
        {
            var connection = this.AmqpConnection;
            if (connection != null)
            {
                await connection.CloseAsync(timeout).ConfigureAwait(false);
            }
        }

        public void Close()
        {
            var connection = this.AmqpConnection;
            if (connection != null)
            {
                connection.Close();
            }
        }

        public AmqpClientSession CreateSession()
        {
            this.AmqpSession = new AmqpClientSession(this);

            return this.AmqpSession;
        }

        void OnConnectionClosed(object o, EventArgs args)
        {
            _isConnectionClosed = true;
        }

        async Task<TransportBase> CreateClientWebSocketTransportAsync(TimeSpan timeout)
        {
            var timeoutHelper = new TimeoutHelper(timeout);
            Uri websocketUri = new Uri(WebSocketConstants.Scheme + this.uri.Host + ":" + this.uri.Port);
            var websocket = await CreateClientWebSocketAsync(websocketUri, timeoutHelper.RemainingTime()).ConfigureAwait(false);
            return new ClientWebSocketTransport(
                websocket,
                null,
                null);
        }

        async Task<ClientWebSocket> CreateClientWebSocketAsync(Uri websocketUri, TimeSpan timeout)
        {
            var websocket = new ClientWebSocket();
            // Set SubProtocol to AMQPWSB10
            websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Amqpwsb10);
            websocket.Options.KeepAliveInterval = TimeSpan.FromMinutes(15);
            websocket.Options.SetBuffer(8 * 1024, 8 * 1024);

            // Check if we're configured to use a proxy server
            //IWebProxy webProxy = WebRequest.DefaultWebProxy;
            //Uri proxyAddress = webProxy != null ? webProxy.GetProxy(websocketUri) : null;
            //if (!websocketUri.Equals(proxyAddress))
            //{
            //    // Configure proxy server
            //    websocket.Options.Proxy = webProxy;
            //}

            if (this.TransportSettings.Certificate != null)
            {
                websocket.Options.ClientCertificates.Add(this.TransportSettings.Certificate);
            }

            using (var cancellationTokenSource = new CancellationTokenSource(timeout))
            {
                try
                {
                    await websocket.ConnectAsync(websocketUri, cancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return websocket;
        }
    }
}