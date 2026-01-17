namespace jira
{
    using SemproJira;


    class Program
    {
        static void Main(string[] args)
        {
            JiraManager jiraManager=new JiraManager();
            jiraManager.GenerateAllOrganisationWorkLogs();
        }
    }
}
