// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Samples;
using Microsoft.Azure.Devices.Client;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Devices.Samples
{
    public class RegistryManagerSample
    {
        private const string DeviceId = "RegistryManagerSample_Device";
        // Either set the IOTHUB_PFX_X509_THUMBPRINT and IOTHUB_PFX_X509_THUMBPRINT2 environment variables 
        // or within launchSettings.json:
        private static string s_primaryThumbprint = Environment.GetEnvironmentVariable("IOTHUB_PFX_X509_THUMBPRINT");
        private static string s_secondaryThumbprint = Environment.GetEnvironmentVariable("IOTHUB_PFX_X509_THUMBPRINT2");
        private string _connectionString;

        private readonly RegistryManager _registryManager;

        public RegistryManagerSample(RegistryManager registryManager, string conectionString)
        {
            _registryManager = registryManager ?? throw new ArgumentNullException(nameof(registryManager));
            _connectionString = conectionString;
        }

        public async Task RunSampleAsync()
        {
            //await EnumerateTwinsAsync().ConfigureAwait(false);

            string deviceId = DeviceId + Guid.NewGuid().ToString();

            try
            {
                Device device = await AddDeviceAsync(deviceId).ConfigureAwait(false);

                string iotHubHostName = GetHostName(_connectionString);
                string deviceConnectionString = $"HostName={iotHubHostName};DeviceId={device.Id};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}";
                var client = DeviceClient.CreateFromConnectionString(deviceConnectionString, Client.TransportType.Amqp);

                var sendSample = new MessageSample(client);
                await sendSample.RunSampleAsync().ConfigureAwait(false);
            }
            finally
            {
                await RemoveDeviceAsync(deviceId).ConfigureAwait(false);
            }
        }

        private static string GetHostName(string iotHubConnectionString)
        {
            Regex regex = new Regex("HostName=([^;]+)", RegexOptions.None);
            return regex.Match(iotHubConnectionString).Groups[1].Value;
        }

        public async Task EnumerateTwinsAsync()
        {
            Console.WriteLine("Querying devices:");

            var query = _registryManager.CreateQuery("select * from devices");

            while (query.HasMoreResults)
            {
                IEnumerable<Twin> twins = await query.GetNextAsTwinAsync().ConfigureAwait(false);

                foreach(Twin twin in twins)
                {
                    Console.WriteLine(
                        "\t{0, -50} : {1, 10} : Last seen: {2, -10}", 
                        twin.DeviceId, 
                        twin.ConnectionState, 
                        twin.LastActivityTime);
                }
            }
        }

        public async Task<Device> AddDeviceAsync(string deviceId)
        {
            Console.Write($"Adding device '{deviceId}' with default authentication . . . ");
            return await _registryManager.AddDeviceAsync(new Device(deviceId)).ConfigureAwait(false);
        }

        public async Task AddDeviceWithSelfSignedCertificateAsync(
            string deviceId,
            string primaryThumbprint,
            string secondaryThumbprint)
        {
            var device = new Device(deviceId)
            {
                Authentication = new AuthenticationMechanism
                {
                    Type = AuthenticationType.SelfSigned,
                    X509Thumbprint = new X509Thumbprint
                    {
                        PrimaryThumbprint = primaryThumbprint,
                        SecondaryThumbprint = secondaryThumbprint
                    }
                }
            };

            Console.Write($"Adding device '{deviceId}' with self signed certificate auth . . . ");
            await _registryManager.AddDeviceAsync(device).ConfigureAwait(false);
            Console.WriteLine("DONE");
        }

        public async Task AddDeviceWithCertificateAuthorityAuthenticationAsync(string deviceId)
        {
            var device = new Device(deviceId)
            {
                Authentication = new AuthenticationMechanism
                {
                    Type = AuthenticationType.CertificateAuthority
                }
            };

            Console.Write($"Adding device '{deviceId}' with CA authentication . . . ");
            await _registryManager.AddDeviceAsync(device).ConfigureAwait(false);
            Console.WriteLine("DONE");
        }

        public async Task RemoveDeviceAsync(string deviceId)
        {
            Console.Write($"Remove device '{deviceId}' . . . ");
            await _registryManager.RemoveDeviceAsync(deviceId);
            Console.WriteLine("Done");
        }

        public async Task UpdateDesiredProperties(string deviceId)
        {
            var twin = await _registryManager.GetTwinAsync(deviceId);

            var patch =
                @"{
                properties: {
                    desired: {
                      customKey: 'customValue'
                    }
                }
            }";

            await _registryManager.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag);
        }
    }
}
