namespace TryMeBitch.Models
{
    public class Issues
    {
        public Guid id { get; set; }
        public string Author { get; set; } 
        public string station { get; set; }
        public string title { get; set; }
        public string summary { get; set; }
        public string Severity { get; set; }    
        public string status { get; set; }
        public DateTime timestamp { get; set; }
        public override string ToString()
        {
            return $"{id} {Author} {station} {title} {summary} {Severity} {status} {timestamp}";
        }

    }
    public class comment
    {
        public Guid id { get; set; }
        public Guid IssueId { get; set; }
        public string Author { get; set; }
        public string station { get; set; }
        public string content { get; set; }
        public DateTime timestamp { get; set; }
        public override string ToString()
        {
            return $"{id} {IssueId} {Author} {station} {content} {timestamp}";
        }
    }
    public class Timeline
    {
        public Guid id { get; set; }
        public Guid IssueId { get; set; }
        public string Author { get; set; }
        public string content { get; set; }
        public DateTime timestamp { get; set; }
    }
}
