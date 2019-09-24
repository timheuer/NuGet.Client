using System;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace NuGet.Protocol.Plugins
{
    internal sealed class EntryAssemblyLogMessage : PluginLogMessage
    {
        private readonly string _fileVersion;
        private readonly string _fullName;
        private readonly string _informationalVersion;

        internal EntryAssemblyLogMessage(DateTimeOffset now)
            : base(now)
        {
            var assembly = Assembly.GetEntryAssembly();
            var informationalVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var fileVersionAttribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();

            _fullName = assembly.FullName;

            if (fileVersionAttribute != null)
            {
                _fileVersion = fileVersionAttribute.Version;
            }

            if (informationalVersionAttribute != null)
            {
                _informationalVersion = informationalVersionAttribute.InformationalVersion;
            }
        }

        public override string ToString()
        {
            var message = new JObject(new JProperty("assembly full name", _fullName));

            if (!string.IsNullOrEmpty(_fileVersion))
            {
                message.Add("file version", _fileVersion);
            }

            if (!string.IsNullOrEmpty(_informationalVersion))
            {
                message.Add("informational version", _informationalVersion);
            }

            return ToString("assembly", message);
        }
    }
}
