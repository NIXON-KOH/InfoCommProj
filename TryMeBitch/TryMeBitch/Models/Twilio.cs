using System;
using System.Collections.Generic;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
namespace TryMeBitch.Models
{
    public class TwilioService
    {

        public void Alert(string title, string number)
        {
            var accountSid = "xxx";
            var authToken = "xxx";
            TwilioClient.Init(accountSid, authToken);

            var messageOptions = new CreateMessageOptions(
              new PhoneNumber($"whatsapp:+65{number}"));
            messageOptions.From = new PhoneNumber("whatsapp:+Phone Number here"); 
            messageOptions.Body = $"An Alert has been Created. {title}";
           


            var message = MessageResource.Create(messageOptions);
            Console.WriteLine(message.Body);
        
    }
}
}
