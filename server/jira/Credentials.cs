using Newtonsoft.Json;
using System.IO;

namespace SemproJira
{
    public class Credentials
    {
        public string Username { get; set; }
        public string ApiToken { get; set; }

        public Credentials()
        {
            LoadFromFile();
        }

        private void LoadFromFile()
        {
            string credentialsPath = @"c:\jira\credentials.json";
            
            if (File.Exists(credentialsPath))
            {
                try
                {
                    string json = File.ReadAllText(credentialsPath);
                    var credentials = JsonConvert.DeserializeObject<CredentialsData>(json);
                    Username = credentials.username;
                    ApiToken = credentials.apiToken;
                }
                catch (System.Exception ex)
                {
                    throw new System.Exception($"Failed to load credentials from {credentialsPath}: {ex.Message}");
                }
            }
            else
            {
                throw new System.Exception($"Credentials file not found at: {credentialsPath}");
            }
        }

        private class CredentialsData
        {
            public string username { get; set; }
            public string apiToken { get; set; }
        }
    }
}
