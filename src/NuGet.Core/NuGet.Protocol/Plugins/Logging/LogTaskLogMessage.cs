// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Common;

namespace NuGet.Protocol.Plugins
{
    internal sealed class LogTaskLogMessage : PluginLogMessage
    {
        private readonly int? _currentTaskId;
        private readonly string _requestId;
        private readonly string _message;
        private readonly LogLevel _logLevel;

        internal LogTaskLogMessage(DateTimeOffset now, string requestId, string message, LogLevel logLevel)
            : base(now)
        {
            _requestId = requestId;
            _currentTaskId = Task.CurrentId;
            _message = message;
            _logLevel = logLevel;
        }

        public override string ToString()
        {
            var message = new JObject(
                new JProperty("request ID", _requestId),
                new JProperty("message", _message),
                new JProperty("logLevel", _logLevel)
                );

            if (_currentTaskId.HasValue)
            {
                message.Add(new JProperty("current task ID", _currentTaskId.Value));
            }

            return ToString("log task", message);
        }
    }
}
