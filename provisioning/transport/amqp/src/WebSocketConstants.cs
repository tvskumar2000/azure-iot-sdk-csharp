namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Amqp
{
    public static class WebSocketConstants
    {
        public const string Scheme = "wss://";
        public const string Version = "13";
        public const int Port = 443;

        internal static class SubProtocols
        {
            public const string Amqpwsb10 = "AMQPWSB10";
        }
    }
}