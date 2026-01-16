//using Microsoft.Data.SqlClient;

namespace Rest_API
{
    public class DataBaseManager
    {
            // security suggestion: load the from a config file (appsettings.json) or environment variable
        string ConnectionString = "Server=192.168.12.110,1433;Database=SemproServer;User Id=sa;Password=MLjRQyeRIq;TrustServerCertificate=True;";
  //      private static SqlConnection conn;

        public DataBaseManager()
        {
    //        conn = new SqlConnection(ConnectionString); // fine for batch-processing few queries
            OpenConnection();
        }

        public void OpenConnection()
        {
      //      conn.Open();
        }

        public void CloseConnection()
        {
        //    conn.Close();
        }

        public void MergeWorklogs(WorklogRecord worklog)
        {
#if false
            // add try/catch in case the merge fails due to constraints or bad data 
            var mergeQuery = @"
        MERGE INTO JiraHours2 AS target
        USING (SELECT 
                    @Worklog_ID AS Worklog_ID, 
                    @TicketKey AS Ticket_Key, 
                    @LinkedKey AS Linked_Key,
                    @Date AS Date, 
                    @LoggedHours AS Logged_Hours, 
                    @Organization AS Organization, 
                    @Classification AS Classification, 
                    @TypeTicket AS Type_Ticket, 
                    @Description AS Description, 
                    @HourType AS Hour_Type,
                    @Author AS Author
               ) AS source
        ON target.Worklog_ID = source.Worklog_ID
        WHEN MATCHED THEN

            UPDATE SET target.Ticket_Key = source.Ticket_Key,
                       target.Linked_Key = source.Linked_Key,
                       target.Date = source.Date,
                       target.Logged_Hours = source.Logged_Hours,
                       target.Organization = source.Organization,
                       target.Classification = source.Classification,
                       target.Type_Ticket = source.Type_Ticket,
                       target.Description = source.Description,
                       target.Hour_Type = source.Hour_Type,
                       target.Author = source.Author
        WHEN NOT MATCHED THEN
            INSERT (Ticket_Key,
                    Linked_Key, Date, 
                    Logged_Hours,
                    Organization, 
                    Classification, 
                    Type_Ticket, 
                    Description, 
                    Hour_Type,
                    Worklog_ID,
                    Author)
            VALUES (source.Ticket_Key,
                    source.Linked_Key,
                    source.Date,
                    source.Logged_Hours,
                    source.Organization, 
                    source.Classification,
                    source.Type_Ticket, 
                    source.Description, 
                    source.Hour_Type, 
                    source.Worklog_ID,
                    source.Author)";


            using var command = new SqlCommand(mergeQuery, conn);
            // Voeg parameters toe voor de waarden in de WorklogRecord
            command.Parameters.AddWithValue("@Worklog_ID", worklog.WorkLogID);
            command.Parameters.AddWithValue("@TicketKey", worklog.IssueKey);
            command.Parameters.AddWithValue("@LinkedKey", worklog.LinkedIssueKey);
            command.Parameters.AddWithValue("@Date", worklog.WorkLogDate);
            command.Parameters.AddWithValue("@LoggedHours", worklog.TimeSpent);
            command.Parameters.AddWithValue("@Organization", worklog.Organization ?? string.Empty);
            command.Parameters.AddWithValue("@Classification", worklog.Classification ?? string.Empty);
            command.Parameters.AddWithValue("@TypeTicket", worklog.TypeOfTicket ?? string.Empty);
            command.Parameters.AddWithValue("@Description", worklog.Comment ?? string.Empty);
            command.Parameters.AddWithValue("@HourType", worklog.HourType ?? string.Empty);
            command.Parameters.AddWithValue("@Author", worklog.Author ?? string.Empty);


            // Try executing, catch if it fails and return detailed error message if it failed.
            try
            {
                command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Error {ex.Message}");
            }
#endif 
        }
        public void MergeNewTickets(WorklogRecord worklog)                  
        {
#if false
            var mergeQuery = @"
        MERGE INTO JiraHours2 AS target
        USING (SELECT 
                    @Worklog_ID AS Worklog_ID, 
                    @TicketKey AS Ticket_Key, 
                    @LinkedKey AS Linked_Key,
                    @Date AS Date, 
                    @LoggedHours AS Logged_Hours, 
                    @Organization AS Organization, 
                    @Classification AS Classification, 
                    @TypeTicket AS Type_Ticket, 
                    @Description AS Description, 
                    @HourType AS Hour_Type,
                    @Author AS Author
               ) AS source
        ON target.Ticket_Key = source.Ticket_Key
        WHEN MATCHED THEN

            UPDATE SET target.Ticket_Key = source.Ticket_Key,
                       target.Linked_Key = source.Linked_Key,
                       target.Date = source.Date,
                       target.Logged_Hours = source.Logged_Hours,
                       target.Organization = source.Organization,
                       target.Classification = source.Classification,
                       target.Type_Ticket = source.Type_Ticket,
                       target.Description = source.Description,
                       target.Hour_Type = source.Hour_Type,
                       target.Author = source.Author
        WHEN NOT MATCHED THEN
            INSERT (Ticket_Key,
                    Linked_Key, Date, 
                    Logged_Hours,
                    Organization, 
                    Classification, 
                    Type_Ticket, 
                    Description, 
                    Hour_Type,
                    Worklog_ID,
                    Author)
            VALUES (source.Ticket_Key,
                    source.Linked_Key,
                    source.Date,
                    source.Logged_Hours,
                    source.Organization, 
                    source.Classification,
                    source.Type_Ticket, 
                    source.Description, 
                    source.Hour_Type, 
                    source.Worklog_ID,
                    source.Author);
    ";

            //DateTime tempDate = DateTime.ParseExact(worklog.WorkLogDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            using var command = new SqlCommand(mergeQuery, conn);
            // Voeg parameters toe voor de waarden in de WorklogRecord
            command.Parameters.AddWithValue("@Worklog_ID", worklog.WorkLogID);
            command.Parameters.AddWithValue("@TicketKey", worklog.IssueKey);
            command.Parameters.AddWithValue("@LinkedKey", worklog.LinkedIssueKey);
            command.Parameters.AddWithValue("@Date", worklog.WorkLogDate);
            command.Parameters.AddWithValue("@LoggedHours", worklog.TimeSpent);
            command.Parameters.AddWithValue("@Organization", worklog.Organization ?? string.Empty);
            command.Parameters.AddWithValue("@Classification", worklog.Classification ?? string.Empty);
            command.Parameters.AddWithValue("@TypeTicket", worklog.TypeOfTicket ?? string.Empty);
            command.Parameters.AddWithValue("@Description", worklog.Comment ?? string.Empty);
            command.Parameters.AddWithValue("@HourType", worklog.HourType ?? string.Empty);
            command.Parameters.AddWithValue("@Author", worklog.Author ?? string.Empty);
            
            try
            {
                command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Error {ex.Message}");
            }
#endif 
        }
    }    
}
