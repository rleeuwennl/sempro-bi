using System.Collections.Generic;

namespace Rest_API
{
    public class JiraManager
    {
        private readonly ProcessJiraData JiraData;
        private DataBaseManager mDataBaseManager;
        public JiraManager()
        {
            JiraData = new ProcessJiraData();
            mDataBaseManager = new DataBaseManager();
            MergeJiraData();
        }

        public void MergeJiraData()
        {
            var Data = JiraData.GetJiraData();


            Dictionary<string, List<WorklogRecord>> organisations = new Dictionary<string, List<WorklogRecord>>();

            foreach (WorklogRecord worklogRecord in Data)
            {
                string organisation = worklogRecord.Organization;
                if (!organisations.ContainsKey(organisation))
                {
                    organisations.Add(organisation, new List<WorklogRecord>());
                }

                organisations[organisation].Add(worklogRecord);
            }

            foreach (var organisation in organisations)
            {
                string output = "";
                output += $"Id;Ticket_Key;Linked_Key;Date;Logged_Hours;Organization;Classification;Type_Ticket;Description;Hour_Type\r\n";
                foreach (WorklogRecord w in organisations[organisation.Key])
                {
                    output += $"{w.WorkLogID};{w.IssueKey};{w.LinkedIssueKey};{w.WorkLogDate};{w.TimeSpent};{w.Organization};{w.Classification};{w.TypeOfTicket};{w.Comment};{w.HourType}\r\n";
                }

                System.IO.File.WriteAllText(@"c:\jira\" + organisation.Key + ".csv",output);
            }
        }
    }
}
