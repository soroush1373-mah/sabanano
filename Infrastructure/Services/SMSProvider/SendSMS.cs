using System;
namespace CleanArchitecture.Infrastructure.Services.SMSProvider
{
    public class SendSMS
    {
        public string Message { get; set; }
        public string ToMobileNumber { get; set; }
        public string Code { get; set; }
        public string? PatternCode { get; set; }                     // empty = the login code pattern
        public Dictionary<string, string>? Attributes { get; set; }  // the %name% values of the pattern
    }
}