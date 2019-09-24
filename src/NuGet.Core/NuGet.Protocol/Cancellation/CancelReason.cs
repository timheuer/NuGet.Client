namespace NuGet.Protocol.Cancellation
{
    public class CancelReason
    {
        public string Reason { get; }
        public string StackTrace { get; }

        public CancelReason(string reason, string stackTrace)
        {
            Reason = reason;
            StackTrace = stackTrace;
        }
    }
}