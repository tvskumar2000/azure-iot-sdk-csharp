// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Sasl;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Provisioning.Client.Transport.Http.Models;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Amqp
{
    internal static class AmqpTransportHandler
    {
        public const string clientVersion = "test-client-version";
        public const string apiVersion = "2017-08-31-preview";

        private static async Task<AmqpClientConnection> CreateConnection(Uri uri, ProvisioningSecurityClientSasToken securityClient, string linkendpoint)
        {
            AmqpSettings settings = await CreateAmqpSettings(securityClient, linkendpoint).ConfigureAwait(false);
            return new AmqpClientConnection(uri, settings);
        }

        private static async Task<AmqpSettings> CreateAmqpSettings(ProvisioningSecurityClientSasToken securityClient, string linkendpoint)
        {
            var settings = new AmqpSettings();
            byte[] ekBuffer = await securityClient.GetEndorsementKeyAsync().ConfigureAwait(false);
            byte[] srkBuffer = await securityClient.GetStorageRootKeyAsync().ConfigureAwait(false);

            string ek = Convert.ToBase64String(ekBuffer);
            string srk = Convert.ToBase64String(srkBuffer);

            TpmNetworkCredential credential = new TpmNetworkCredential(ek, srk);

                var saslProvider = new SaslTransportProvider();
                saslProvider.Versions.Add(AmqpConstants.DefaultProtocolVersion);
                settings.TransportProviders.Add(saslProvider);

            SaslTpmHandler tpmHandler = new SaslTpmHandler(ekBuffer, srkBuffer, linkendpoint, securityClient);
            saslProvider.AddHandler(tpmHandler);

            var amqpProvider = new AmqpTransportProvider();
            amqpProvider.Versions.Add(AmqpConstants.DefaultProtocolVersion);
            settings.TransportProviders.Add(amqpProvider);
            return settings;
        }

        internal static async Task<AmqpClientConnection> CreateAmqpCloudConnectionAsync(
            string deviceEndpoint, 
            X509Certificate2 clientCert, 
            string linkEndpoint, 
            bool useWebSocket,
            ProvisioningSecurityClientSasToken securityClient)
        {
            AmqpClientConnection amqpCloudConnection;
            if (useWebSocket)
            {
                // TODO: enable WS
                amqpCloudConnection = await CreateConnection(
                    new Uri(WebSocketConstants.Scheme + deviceEndpoint + ":" + WebSocketConstants.Port),
                    securityClient,
                    linkEndpoint).ConfigureAwait(false);
        }
        else
            {
                amqpCloudConnection = await CreateConnection(
                    new Uri("amqps://" + deviceEndpoint + ":" + AmqpConstants.DefaultSecurePort), 
                    securityClient, 
                    linkEndpoint).ConfigureAwait(false);
            }

            await amqpCloudConnection.OpenAsync(TimeSpan.FromMinutes(1), useWebSocket, clientCert).ConfigureAwait(false);

            var amqpDeviceSession = amqpCloudConnection.CreateSession();
            await amqpDeviceSession.OpenAsync(TimeSpan.FromMinutes(1)).ConfigureAwait(false);

            var amqpReceivingLink = amqpDeviceSession.CreateReceivingLink(linkEndpoint);

            amqpReceivingLink.AddClientVersion(clientVersion);
            amqpReceivingLink.AddApiVersion(apiVersion);

            await amqpReceivingLink.OpenAsync(TimeSpan.FromMinutes(1)).ConfigureAwait(false);

            var amqpSendingLink = amqpDeviceSession.CreateSendingLink(linkEndpoint);

            amqpSendingLink.AddClientVersion(clientVersion);
            amqpSendingLink.AddApiVersion(apiVersion);

            await amqpSendingLink.OpenAsync(TimeSpan.FromMinutes(1)).ConfigureAwait(false);

            return amqpCloudConnection;
        }

        public static async Task<RegistrationOperationStatus> RegisterDeviceAsync(AmqpClientConnection client, string correlationId)
        {
            var amqpMessage = AmqpMessage.Create(new MemoryStream(Encoding.ASCII.GetBytes(DeviceOperations.Register)), false);
            amqpMessage.Properties.CorrelationId = correlationId;
            amqpMessage.ApplicationProperties.Map[MessageApplicationPropertyNames.OperationType] = DeviceOperations.Register;
            amqpMessage.ApplicationProperties.Map[MessageApplicationPropertyNames.ForceRegistration] = false;
            var outcome = await client.AmqpSession.SendingLink.SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()), TimeSpan.FromMinutes(1)).ConfigureAwait(false);
            var amqpResponse = await client.AmqpSession.ReceivingLink.ReceiveMessageAsync(TimeSpan.FromMinutes(1)).ConfigureAwait(false);
            client.AmqpSession.ReceivingLink.AcceptMessage(amqpResponse);
            string jsonResponse = await new StreamReader(amqpResponse.BodyStream).ReadToEndAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<RegistrationOperationStatus>(jsonResponse);
        }

        public static async Task<RegistrationOperationStatus> OperationStatusLookupAsync(AmqpClientConnection client, string operationId, string correlationId)
        {
            var amqpMessage = AmqpMessage.Create(new MemoryStream(Encoding.ASCII.GetBytes(DeviceOperations.GetOperationStatus)), false);
            amqpMessage.Properties.CorrelationId = correlationId;
            amqpMessage.ApplicationProperties.Map[MessageApplicationPropertyNames.OperationType] = DeviceOperations.GetOperationStatus;
            amqpMessage.ApplicationProperties.Map[MessageApplicationPropertyNames.OperationId] = operationId;
            var outcome = await client.AmqpSession.SendingLink.SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()), TimeSpan.FromMinutes(1)).ConfigureAwait(false);
            var amqpResponse = await client.AmqpSession.ReceivingLink.ReceiveMessageAsync(TimeSpan.FromMinutes(1)).ConfigureAwait(false);
            client.AmqpSession.ReceivingLink.AcceptMessage(amqpResponse);
            string jsonResponse = await new StreamReader(amqpResponse.BodyStream).ReadToEndAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<RegistrationOperationStatus>(jsonResponse);
        }

        public static ProvisioningRegistrationResult ConvertToProvisioningRegistrationResult(
            DeviceRegistrationResult result)
        {
            var status = ProvisioningRegistrationStatusType.Failed;
            Enum.TryParse(result.Status, true, out status);

            return new ProvisioningRegistrationResultTpm(
                result.RegistrationId,
                result.CreatedDateTimeUtc,
                result.AssignedHub,
                result.DeviceId,
                status,
                result.GenerationId,
                result.LastUpdatedDateTimeUtc,
                result.ErrorCode == null ? 0 : (int)result.ErrorCode,
                result.ErrorMessage,
                result.Etag,
                result.Tpm.AuthenticationKey);
        }
    }
}
