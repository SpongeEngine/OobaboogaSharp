namespace SpongeEngine.OobaboogaSharp.Models.Common
{
    public class Exception : System.Exception
    {
        public string Provider { get; }
        public int StatusCode { get; }
        public string ResponseContent { get; }

        public Exception(string message, string provider, int statusCode, string responseContent) 
            : base(message)
        {
            Provider = provider;
            StatusCode = statusCode;
            ResponseContent = responseContent;
        }
    }
}