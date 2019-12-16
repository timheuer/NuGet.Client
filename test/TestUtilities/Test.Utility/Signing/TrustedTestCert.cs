// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using NuGet.Common;
using NuGet.Test.Utility;

namespace Test.Utility.Signing
{
    public static class TrustedTestCert
    {
        public static TrustedTestCert<X509Certificate2> Create(
            X509Certificate2 cert,
            StoreName storeName = StoreName.TrustedPeople,
            StoreLocation storeLocation = StoreLocation.CurrentUser,
            TimeSpan? maximumValidityPeriod = null)
        {
            return new TrustedTestCert<X509Certificate2>(
                cert,
                x => x,
                storeName,
                storeLocation,
                maximumValidityPeriod);
        }
    }

    /// <summary>
    /// Give a certificate full trust for the life of the object.
    /// </summary>
    public class TrustedTestCert<T> : IDisposable
    {
        private X509Store _store;

        public X509Certificate2 TrustedCert { get; }

        public T Source { get; }

        public StoreName StoreName { get; }

        public StoreLocation StoreLocation { get; }

        private bool _isDisposed;

        public TrustedTestCert(T source,
            Func<T, X509Certificate2> getCert,
            StoreName storeName = StoreName.TrustedPeople,
            StoreLocation storeLocation = StoreLocation.CurrentUser,
            TimeSpan? maximumValidityPeriod = null)
        {
            Source = source;
            TrustedCert = getCert(source);

            if (!maximumValidityPeriod.HasValue)
            {
                maximumValidityPeriod = TimeSpan.FromHours(2);
            }

#if IS_DESKTOP
            if (TrustedCert.NotAfter - TrustedCert.NotBefore > maximumValidityPeriod.Value)
            {
                throw new InvalidOperationException($"The certificate used is valid for more than {maximumValidityPeriod}.");
            }
#endif
            if (RuntimeEnvironmentHelper.IsMacOSX)
            {
                AddTrustedCert();
            }
            else
            {
                StoreName = storeName;
                StoreLocation = storeLocation;
                AddCertificateToStore();  
            }
            ExportCrl();

        }

        private void AddCertificateToStore()
        {
            _store = new X509Store(StoreName, StoreLocation);
            _store.Open(OpenFlags.ReadWrite);
            _store.Add(TrustedCert);
        }

        //According to https://github.com/dotnet/corefx/blob/master/Documentation/architecture/cross-platform-cryptography.md#x509store,
        //on macOS the X509Store class is a projection of system trust decisions (read-only), user trust decisions (read-only), and user key storage (read-write).
        //So we have to run command to add certificate to System.keychain to make it trusted.
        private void AddTrustedCert()
        {
            var certFile = new System.IO.FileInfo(System.IO.Path.Combine("/tmp", $"{TrustedCert.Thumbprint}.cer"));

            System.IO.File.WriteAllBytes(certFile.FullName, TrustedCert.RawData);

            string addToKeyChainCmd = $"sudo security add-trusted-cert -d -r trustRoot " +
                                      $"-k \"/Library/Keychains/System.keychain\" " +
                                      $"\"{certFile.FullName}\"";

            RunMacCommand(addToKeyChainCmd);

        }

        //According to https://github.com/dotnet/corefx/blob/master/Documentation/architecture/cross-platform-cryptography.md#x509store,
        //on macOS the X509Store class is a projection of system trust decisions (read-only), user trust decisions (read-only), and user key storage (read-write).
        //So we have to run command to remove certificate from System.keychain to make it untrusted.
        private void RemoveTrustedCert()
        {
            var certFile = new System.IO.FileInfo(System.IO.Path.Combine("/tmp", $"{TrustedCert.Thumbprint}.cer"));

            string removeFromKeyChainCmd = $"sudo security delete-certificate -Z {TrustedCert.Thumbprint} /Library/Keychains/System.keychain";

            RunMacCommand(removeFromKeyChainCmd);

            File.Delete(certFile.FullName);
        }

        private void RunMacCommand(string cmd)
        {
            string workingDirectory = "/bin";
            string args = "-c \"" + cmd + "\"";

            var result = CommandRunner.Run("/bin/bash",
                workingDirectory,
                args,
                waitForExit: true,
                timeOutInMilliseconds: 60000);

            if (result.Item1 != 0)
            {
                throw new Exception($"Run security command failed with following log information :\n {result.AllOutput} \n" +
                                    $"exit code =   {result.Item1} \n" +
                                    $"exit output = {result.Item2} \n" +
                                    $"exit error =  {result.Item3} \n");
            }            
        }

        private void ExportCrl()
        {
            var testCertificate = Source as TestCertificate;

            if (testCertificate != null && testCertificate.Crl != null)
            {
                testCertificate.Crl.ExportCrl();
            }
        }

        private void DisposeCrl()
        {
            var testCertificate = Source as TestCertificate;

            if (testCertificate != null && testCertificate.Crl != null)
            {
                testCertificate.Crl.Dispose();
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (RuntimeEnvironmentHelper.IsMacOSX)
                {
                    RemoveTrustedCert();
                }
                else
                {
                    using (_store)
                    {
                        _store.Remove(TrustedCert);
                    }
                }
                
                DisposeCrl();

                TrustedCert.Dispose();

                _isDisposed = true;
            }
        }
    }
}