using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace NuGet.CommandLine.Common
{
    [Serializable]
    class SystemToolException : Exception
    {
        public SystemToolException()
        {
        }

        public SystemToolException(string message)
            : base(message)
        {
        }

        public SystemToolException(string format, params object[] args)
            : base(string.Format(CultureInfo.CurrentCulture, format, args))
        {
        }

        public SystemToolException(string message, Exception exception)
            : base(message, exception)
        {
        }

        protected SystemToolException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
