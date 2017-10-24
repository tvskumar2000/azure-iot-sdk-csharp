using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Amqp
{
    static class Utils
    {
        static Utils()
        {
        }

        public static void ValidateBufferBounds(byte[] buffer, int offset, int size)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            ValidateBufferBounds(buffer.Length, offset, size);
        }
        static void ValidateBufferBounds(int bufferSize, int offset, int size)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", "Buffer offset is less than 0");
            }

            if (offset > bufferSize)
            {
                throw new ArgumentOutOfRangeException("offset", "Buffer offset is larger than bufferSize");
            }

            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException("size", "Size is less than or equal to 0");
            }

            int remainingBufferSpace = bufferSize - offset;
            if (size > remainingBufferSpace)
            {
                throw new ArgumentOutOfRangeException("size", "Size is larger than the remaining buffer space");
            }
        }
    }
}
