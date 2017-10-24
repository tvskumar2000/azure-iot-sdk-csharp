// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Amqp
{
    public static class StringFormattingExtensions
    {
        public static string FormatForUser(this string format, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, format, args);
        }

        public static string FormatInvariant(this string format, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }

#pragma warning disable CA1801 // Review unused parameters
        public static string FormatErrorForUser(this string message, string activityId, int errorCode)
#pragma warning restore CA1801 // Review unused parameters
        {
            return string.Format(CultureInfo.InvariantCulture,
                "If you contact a support representative please include this correlation identifier: {1}, timestamp: {2:u}, errorcode: IH{3}",
                activityId, DateTime.UtcNow, errorCode);
        }

        public static string Truncate(this string message, int maximumSize)
        {
            return message.Length > maximumSize ? message.Substring(0, maximumSize) + "...(truncated)" : message;
        }
    }
}
