namespace TryMeBitch.Models
{
    public class AlertEmailConfig
    {
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string To { get; set; }
    }

}
