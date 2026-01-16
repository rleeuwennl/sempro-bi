namespace Rest_API
{
    public class WorklogRecord
    {
        public string IssueKey { get; set; } = ""; // (= "") are default values that prevent nulls and are great for db
        public string LinkedIssueKey { get; set; } = "";
        public string Organization { get; set; } = "";
        public string Classification { get; set; } = "";
        public string TypeOfTicket { get; set; } = "";
        public string Author { get; set; } = "";
        public double TimeSpent { get; set; } = new double();
        public string WorkLogDate { get; set; } = "";
        public string WorkLogID { get; set; } = "";
        public string Comment { get; set; } = "";
        public string HourType { get; set; } = "";
    }
}