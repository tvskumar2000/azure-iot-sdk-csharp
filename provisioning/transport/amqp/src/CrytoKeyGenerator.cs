using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Amqp
{
    public static class CryptoKeyGenerator
    {
        // Function to Generate a 64 bits Key.
        public static string GenerateKey(int maxNumber)
        {
            var builder = new StringBuilder();

            //// TODO v-renesc 2016-08-15 Discuss with Russell... this code uses a non-threadsafe and cryptographically weak random number generator.
            var random = new Random();
            for (var i = 0; i < maxNumber; i++)
            {
                var ch = Convert.ToChar(Convert.ToInt32(Math.Floor((26 * random.NextDouble()) + 65)));
                builder.Append(ch);
            }
            return builder.ToString();
        }
    }
}
