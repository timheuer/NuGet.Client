// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using NuGet.Common;

namespace NuGet.CommandLine
{
    class ProtocolDiagnosticsWriter : IProtocolDiagnostics, IDisposable
    {
        object _lock;
        StreamWriter _file;
        JsonWriter _jsonWriter;

        public ProtocolDiagnosticsWriter(string filename)
        {
            _lock = new object();

            _file = new StreamWriter(filename);
            _jsonWriter = new JsonTextWriter(_file);
            _jsonWriter.WriteStartArray();
        }

        public void Dispose()
        {
            _jsonWriter?.WriteEndArray();
            _jsonWriter?.Flush();
            _jsonWriter = null;

            _file?.Flush();
            _file?.Dispose();
            _file = null;
        }

        public void OnEvent(
            string source,
            string url,
            TimeSpan? headerDuration,
            TimeSpan requestDuration,
            int? httpStatucCode,
            long bodyBytes,
            bool isSuccess,
            bool isRetry,
            bool isCancelled)
        {
            var timestamp = DateTime.UtcNow.ToString("o");
            lock (_lock)
            {
                _jsonWriter.WriteStartObject();

                _jsonWriter.WritePropertyName("timestamp");
                _jsonWriter.WriteValue(timestamp);

                _jsonWriter.WritePropertyName("source");
                _jsonWriter.WriteValue(source);

                _jsonWriter.WritePropertyName("url");
                _jsonWriter.WriteValue(url);

                if (headerDuration.HasValue)
                {
                    _jsonWriter.WritePropertyName("headerDuration");
                    _jsonWriter.WriteValue(headerDuration.Value.TotalMilliseconds);
                }

                _jsonWriter.WritePropertyName("requestDuration");
                _jsonWriter.WriteValue(requestDuration.TotalMilliseconds);

                if (httpStatucCode.HasValue)
                {
                    _jsonWriter.WritePropertyName("httpStatusCode");
                    _jsonWriter.WriteValue(httpStatucCode.Value);
                }

                _jsonWriter.WritePropertyName("bodyBytes");
                _jsonWriter.WriteValue(bodyBytes);

                _jsonWriter.WritePropertyName("isSuccess");
                _jsonWriter.WriteValue(isSuccess);

                _jsonWriter.WritePropertyName("isRetry");
                _jsonWriter.WriteValue(isRetry);

                _jsonWriter.WritePropertyName("isCancelled");
                _jsonWriter.WriteValue(isCancelled);

                _jsonWriter.WriteEndObject();

                // just in case program crashes or ctrl-c is pressed.
                _jsonWriter.Flush();
            }
        }
    }

    class ProtocolDiagnosticsService : NuGet.Common.INuGetTelemetryService
    {
        private readonly IProtocolDiagnostics _protocolDiagnosticsWriter;

        public ProtocolDiagnosticsService(IProtocolDiagnostics protocolDiagnosticsWriter)
        {
            _protocolDiagnosticsWriter = protocolDiagnosticsWriter;
        }

        public IProtocolDiagnostics CreateProtocolDiagnostics(TelemetryActivity telemetryActivity)
        {
            return new ProtocolDiagnosticsDelegate(_protocolDiagnosticsWriter);
        }

        public void EmitTelemetryEvent(TelemetryEvent telemetryData)
        {
        }
    }

    class ProtocolDiagnosticsDelegate : IProtocolDiagnostics
    {
        IProtocolDiagnostics _inner;

        public ProtocolDiagnosticsDelegate(IProtocolDiagnostics inner)
        {
            _inner = inner;
        }

        public void OnEvent(string source,
            string url,
            TimeSpan? headerDuration,
            TimeSpan requestDuration,
            int? httpStatusCode,
            long bodyBytes,
            bool isSuccess,
            bool isRetry,
            bool isCancelled)
        {
            _inner.OnEvent(source, url, headerDuration, requestDuration, httpStatusCode, bodyBytes, isSuccess, isRetry, isCancelled);
        }
    }
}
