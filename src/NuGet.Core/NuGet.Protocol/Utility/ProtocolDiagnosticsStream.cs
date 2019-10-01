// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;

namespace NuGet.Protocol.Utility
{
    internal class ProtocolDiagnosticsStream : Stream
    {
        private IProtocolDiagnostics _protocolDiagnostics;
        private string _source;
        private string _url;
        private bool _isRetry;
        private TimeSpan? _headerDuration;
        private Stopwatch _requestDuration;
        int? _httpStatusCode;
        private long _bytesRead;

        public ProtocolDiagnosticsStream(Stream baseStream,
            IProtocolDiagnostics protocolDiagnostics,
            string source,
            string url,
            bool isRetry,
            TimeSpan? headerDuration,
            Stopwatch requestDuration,
            int? httpStatusCode)
        {
            BaseStream = baseStream;
            _protocolDiagnostics = protocolDiagnostics;
            _source = source;
            _url = url;
            _isRetry = isRetry;
            _headerDuration = headerDuration;
            _requestDuration = requestDuration;
            _httpStatusCode = httpStatusCode;
        }

        public Stream BaseStream { get; }

        public override bool CanRead => BaseStream.CanRead;

        public override bool CanSeek => BaseStream.CanSeek;

        public override bool CanWrite => BaseStream.CanWrite;

        public override long Length => BaseStream.Length;

        public override long Position { get => BaseStream.Position; set => BaseStream.Position = value; }

        public override void Flush()
        {
            BaseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                var read = BaseStream.Read(buffer, offset, count);
                if (read > 0)
                {
                    _bytesRead += read;
                }
                else
                {
                    RaiseDiagnosticEvent(isSuccess: true);
                }
                return read;
            }
            catch
            {
                RaiseDiagnosticEvent(isSuccess: false);
                throw;
            }
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                var read = await base.ReadAsync(buffer, offset, count, cancellationToken);
                if (read > 0)
                {
                    _bytesRead += read;
                }
                else
                {
                    RaiseDiagnosticEvent(isSuccess: true);
                }
                return read;
            }
            catch
            {
                RaiseDiagnosticEvent(isSuccess: false);
                throw;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return BaseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            BaseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            BaseStream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            RaiseDiagnosticEvent(isSuccess: true);
        }

        private void RaiseDiagnosticEvent(bool isSuccess)
        {
            if (_protocolDiagnostics != null)
            {
                _protocolDiagnostics.OnEvent(
                    _source,
                    _url,
                    _headerDuration,
                    _requestDuration.Elapsed,
                    _httpStatusCode,
                    _bytesRead,
                    isSuccess,
                    _isRetry,
                    isCancelled: false);

                _protocolDiagnostics = null;
            }
        }
    }
}
