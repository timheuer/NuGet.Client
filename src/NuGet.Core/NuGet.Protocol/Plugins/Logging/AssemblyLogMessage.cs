// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using NuGet.Common;

namespace NuGet.Protocol.Plugins
{
    internal sealed class AssemblyLogMessage : PluginLogMessage
    {
        private readonly string _fileVersion;
        private readonly string _fullName;
        private readonly string _informationalVersion;
        private readonly string _location;
        private readonly string _os;
        internal AssemblyLogMessage(DateTimeOffset now)
            : base(now)
        {
            var assembly = typeof(PluginFactory).Assembly;
            var informationalVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var fileVersionAttribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();

            _fullName = assembly.FullName;
            _location = assembly.Location;

            if (fileVersionAttribute != null)
            {
                _fileVersion = fileVersionAttribute.Version;
            }

            if (informationalVersionAttribute != null)
            {
                _informationalVersion = informationalVersionAttribute.InformationalVersion;
            }

            if (RuntimeEnvironmentHelper.IsWindows)
            {
                _os = "Windows";
            }

            if (RuntimeEnvironmentHelper.IsMacOSX)
            {
                _os = "Mac";
            }

            if (RuntimeEnvironmentHelper.IsLinux)
            {
                _os = "Linux";
            }
        }

        public override string ToString()
        {
            var message = new JObject(new JProperty("assembly full name", _fullName));

            if (!string.IsNullOrEmpty(_fileVersion))
            {
                message.Add("file version", _fileVersion);
            }

            if (!string.IsNullOrEmpty(_location))
            {
                message.Add("location", _location);
            }

            if (!string.IsNullOrEmpty(_os))
            {
                message.Add("operating system", _os);
            }

            if (!string.IsNullOrEmpty(_informationalVersion))
            {
                message.Add("informational version", _informationalVersion);
            }

            return ToString("assembly", message);
        }
    }
}
