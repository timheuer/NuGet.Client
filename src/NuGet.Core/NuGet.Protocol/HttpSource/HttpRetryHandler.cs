// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol.Utility;

namespace NuGet.Protocol
{
    /// <summary>
    /// The <see cref="HttpRetryHandler"/> is for retrying and HTTP request if it times out, has any exception,
    /// or returns a status code of 500 or greater.
    /// </summary>
    public class HttpRetryHandler : IHttpRetryHandler
    {
        /// <summary>
        /// Make an HTTP request while retrying after failed attempts or timeouts.
        /// </summary>
        /// <remarks>
        /// This method accepts a factory to create instances of the <see cref="HttpRequestMessage"/> because
        /// requests cannot always be used. For example, suppose the request is a POST and contains content
        /// of a stream that can only be consumed once.
        /// </remarks>
        [Obsolete("Use the overload with " + nameof(IProtocolDiagnostics) + ". Use " + nameof(NullProtocolDiagnostics) + " if no diagnostics are needed")]
        public Task<HttpResponseMessage> SendAsync(
            HttpRetryHandlerRequest request,
            ILogger log,
            CancellationToken cancellationToken)
        {
            return SendAsync(request, log, source: string.Empty, NullProtocolDiagnostics.Instance, cancellationToken);
        }

        /// <summary>
        /// Make an HTTP request while retrying after failed attempts or timeouts.
        /// </summary>
        /// <remarks>
        /// This method accepts a factory to create instances of the <see cref="HttpRequestMessage"/> because
        /// requests cannot always be used. For example, suppose the request is a POST and contains content
        /// of a stream that can only be consumed once.
        /// </remarks>

        public async Task<HttpResponseMessage> SendAsync(
            HttpRetryHandlerRequest request,
            ILogger log,
            string source,
            IProtocolDiagnostics protocolDiagnostics,
            CancellationToken cancellationToken)
        {
            var tries = 0;
            HttpResponseMessage response = null;
            var success = false;

            while (tries < request.MaxTries && !success)
            {
                if (tries > 0)
                {
                    await Task.Delay(request.RetryDelay, cancellationToken);
                }

                tries++;
                success = true;

                using (var requestMessage = request.RequestFactory())
                {
                    var stopwatch = Stopwatch.StartNew();
                    var headerStopwatch = new Stopwatch();
                    var requestUri = requestMessage.RequestUri.ToString();
                    
                    try
                    {
                        // The only time that we will be disposing this existing response is if we have 
                        // successfully fetched an HTTP response but the response has an status code indicating
                        // failure (i.e. HTTP status code >= 500).
                        // 
                        // If we don't even get an HTTP response message because an exception is thrown, then there
                        // is no response instance to dispose. Additionally, we cannot use a finally here because
                        // the caller needs the response instance returned in a non-disposed state.
                        //
                        // Also, remember that if an HTTP server continuously returns a failure status code (like
                        // 500 Internal Server Error), we will retry some number of times but eventually return the
                        // response as-is, expecting the caller to check the status code as well. This results in the
                        // success variable being set to false but the response being returned to the caller without
                        // disposing it.
                        response?.Dispose();

                        // Add common headers to the request after it is created by the factory. This includes
                        // X-NuGet-Session-Id which is added to all nuget requests.
                        foreach (var header in request.AddHeaders)
                        {
                            requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }

                        log.LogInformation("  " + string.Format(
                            CultureInfo.InvariantCulture,
                            Strings.Http_RequestLog,
                            requestMessage.Method,
                            requestUri));

                        // Issue the request.
                        var timeoutMessage = string.Format(
                            CultureInfo.CurrentCulture,
                            Strings.Http_Timeout,
                            requestMessage.Method,
                            requestUri,
                            (int)request.RequestTimeout.TotalMilliseconds);
                        response = await TimeoutUtility.StartWithTimeout(
                            async timeoutToken =>
                                {
                                    headerStopwatch.Restart();
                                    var result = await request.HttpClient.SendAsync(requestMessage, request.CompletionOption, timeoutToken);
                                    headerStopwatch.Stop();
                                    return result;
                                },
                            request.RequestTimeout,
                            timeoutMessage,
                            cancellationToken);

                        // Wrap the response stream so that the download can timeout.
                        if (response.Content != null)
                        {
                            var networkStream = await response.Content.ReadAsStreamAsync();
                            var timeoutStream = new DownloadTimeoutStream(requestUri, networkStream, request.DownloadTimeout);
                            var diagnosticsStream = new ProtocolDiagnosticsStream(timeoutStream, protocolDiagnostics, source, requestUri, isRetry: tries > 1, headerStopwatch.Elapsed, stopwatch, (int)response.StatusCode);

                            var newContent = new StreamContent(diagnosticsStream);

                            // Copy over the content headers since we are replacing the HttpContent instance associated
                            // with the response message.
                            foreach (var header in response.Content.Headers)
                            {
                                newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
                            }

                            response.Content = newContent;
                        }

                        //stopwatch.Stop();
                        log.LogInformation("  " + string.Format(
                            CultureInfo.InvariantCulture,
                            Strings.Http_ResponseLog,
                            response.StatusCode,
                            requestUri,
                            stopwatch.ElapsedMilliseconds));

                        if ((int)response.StatusCode >= 500)
                        {
                            success = false;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        stopwatch.Stop();
                        response?.Dispose();

                        protocolDiagnostics.OnEvent(source, requestMessage.RequestUri.ToString(), headerStopwatch.Elapsed, stopwatch.Elapsed, httpStatusCode: null, bodyBytes: 0, false, tries > 1, true);

                        throw;
                    }
                    catch (Exception e)
                    {
                        stopwatch.Stop();
                        success = false;

                        response?.Dispose();

                        protocolDiagnostics.OnEvent(source, requestMessage.RequestUri.ToString(), headerStopwatch.Elapsed, stopwatch.Elapsed, null, 0, success, tries > 1, false);

                        if (tries >= request.MaxTries)
                        {
                            throw;
                        }

                        log.LogInformation(string.Format(
                            CultureInfo.CurrentCulture,
                            Strings.Log_RetryingHttp,
                            requestMessage.Method,
                            requestUri,
                            requestMessage)
                            + Environment.NewLine
                            + ExceptionUtilities.DisplayMessage(e));
                    }
                }
            }

            return response;
        }
    }
}
