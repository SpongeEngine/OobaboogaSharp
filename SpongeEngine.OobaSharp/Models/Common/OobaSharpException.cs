﻿namespace SpongeEngine.OobaSharp.Models.Common
{
    public class OobaSharpException : Exception
    {
        public string Provider { get; }
        public int StatusCode { get; }
        public string ResponseContent { get; }

        public OobaSharpException(string message, string provider, int statusCode, string responseContent) 
            : base(message)
        {
            Provider = provider;
            StatusCode = statusCode;
            ResponseContent = responseContent;
        }
    }
}