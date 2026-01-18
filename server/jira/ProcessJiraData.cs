using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace SemproJira
{
    public class ProcessJiraData
    {
        // security suggestion: avoid static fields for credentials (fine for now), better to Inject through constructor parameters
        static readonly string baseUrl = "https://sempro-technologies.atlassian.net"; // Jira cload instance root
        static string username; // credentials from credentials.json
        static string apiToken;
        static readonly HttpClient client = new HttpClient(); // Shared across all API calls

        public ProcessJiraData()
        {
            var credentials = new Credentials();
            username = credentials.Username;
            apiToken = credentials.ApiToken;


            byte[] byteArray = Encoding.ASCII.GetBytes($"{username}:{apiToken}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


        }
        public List<WorklogRecord> GetJiraData()
        {
            List<WorklogRecord> worklogRecords = new List<WorklogRecord>();
            var list = CollectJiraTickets();

            // Reduced list!!
            int l = list.Count;
            //list.RemoveRange(1000, l - 1000);
            //Console.WriteLine($"Using reduced list of {list.Count}   !!!!!!!!!!!!!!!!!!!!!!!!");

            int countDown = list.Count;
            foreach (var issue in list)
            {
                dynamic tempTicket = JiraIssue(issue.id.Value);
                string projectKey = tempTicket.fields.project.key;

                if (projectKey == "SP" || projectKey == "SOF" || projectKey == "ES")
                {
                    string organization = string.Empty;
                    string classification = string.Empty;
                    string typeOfTicket = string.Empty;
                    string author = string.Empty;
                    double timeSpent = new double();
                    string workLogID = string.Empty;
                    DateTime workLogDate = DateTime.MinValue;
                    DateTime created = DateTime.MinValue;
                    string comment = string.Empty;
                    string linkedIssueKey = string.Empty;
                    string hourType = string.Empty;
                    string issueKey = tempTicket.key.Value;

                    dynamic serviceTicket = null;
                    dynamic worklogs = null;

                    if (projectKey == "SP")
                    {
                        serviceTicket = tempTicket;
                        created = serviceTicket.fields.created;

                        if (serviceTicket.fields.customfield_10002 != null && serviceTicket.fields.customfield_10002.Count > 0)
                            organization = serviceTicket.fields.customfield_10002[0]?.name ?? string.Empty;

                        if (serviceTicket.fields.customfield_10105 != null)
                            typeOfTicket = serviceTicket.fields.customfield_10105.value ?? string.Empty;

                        if (serviceTicket?.fields?.customfield_10063 != null)
                            classification = serviceTicket.fields.customfield_10063.value ?? string.Empty;

                        if (serviceTicket.fields.issuelinks != null && serviceTicket.fields.issuelinks.Count > 0)
                        {
                            var linkKeys = new List<string>();
                            foreach (var linkedKey in serviceTicket.fields.issuelinks)
                            {
                                string tempLinkedKey = linkedKey.outwardIssue?.key ?? string.Empty;
                                linkKeys.Add(tempLinkedKey);
                            }
                            linkedIssueKey = string.Join(";", linkKeys);
                        }
                    }

                    if (projectKey == "SOF" || projectKey == "ES")
                    {
                        serviceTicket = tempTicket;
                        created = serviceTicket.fields.created;

                        if (serviceTicket.fields.issuelinks != null && serviceTicket.fields.issuelinks.Count > 0)
                            linkedIssueKey = serviceTicket.fields.issuelinks?[0].inwardIssue?.key ?? string.Empty;

                        var linkedIssueData = JiraIssue(linkedIssueKey);

                        if (linkedIssueData?.fields?.customfield_10002 != null && linkedIssueData.fields.customfield_10002.Count > 0)
                            organization = linkedIssueData.fields.customfield_10002?[0]?.name ?? string.Empty;

                        if (linkedIssueData?.fields?.customfield_10063 != null)
                            classification = linkedIssueData.fields.customfield_10063.value ?? string.Empty;

                        if (linkedIssueData?.fields?.customfield_10105 != null)
                            typeOfTicket = linkedIssueData.fields.customfield_10105.value ?? string.Empty;
                    }

                    switch (projectKey)
                    {
                        case "SOF":
                            hourType = "Software";
                            break;
                        case "ES":
                            hourType = "Mechanical";
                            break;
                        case "SP":
                            hourType = (typeOfTicket == "Visit") ? "Visit" : "Mechanical";
                            break;
                    }

                    worklogs = GetWorkLogs(issueKey);

                    if (worklogs !=null && serviceTicket.fields.worklog.worklogs != null && serviceTicket.fields.worklog.worklogs.Count > 0)
                    {                      

                        foreach (var worklog in worklogs.worklogs)
                        {
                            author = worklog.author.displayName;
                            timeSpent = Convert.ToDouble(worklog.timeSpentSeconds) / 3600;
                            workLogID = worklog.id;
                            string dateString = worklog.started;
                            DateTime.TryParse(dateString, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out workLogDate);
                            comment = string.Empty;

                            WorklogRecord record = new WorklogRecord()
                            {
                                IssueKey = issueKey,
                                LinkedIssueKey = linkedIssueKey,
                                Organization = organization,
                                Classification = classification,
                                TypeOfTicket = typeOfTicket,
                                Author = author,
                                TimeSpent = timeSpent,
                                WorkLogDate = workLogDate,
                                WorkLogID = workLogID,
                                HourType = hourType,
                                Comment = comment
                            };

                            worklogRecords.Add(record);
                        }
                    }
                    else
                    {
                        WorklogRecord record = new WorklogRecord()
                        {
                            IssueKey = issueKey,
                            LinkedIssueKey = linkedIssueKey,
                            Organization = organization,
                            Classification = classification,
                            TypeOfTicket = typeOfTicket,
                            Author = "",
                            TimeSpent = 0,
                            WorkLogDate = created,
                            WorkLogID = "",
                            HourType = hourType,
                            Comment = ""
                        };

                        worklogRecords.Add(record);
                    }
                }
            }


            return worklogRecords;
        }


        public dynamic GetWorkLogs(string issueKey)
        {
            // Define cache directory and file path
            string cacheDir = @"c:\jira\worklogs";
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }
            
            string cacheFilePath = Path.Combine(cacheDir, $"{issueKey}_worklogs.json");
            
            // Check if cache file exists
            if (File.Exists(cacheFilePath))
            {
                try
                {
                    string cachedJson = File.ReadAllText(cacheFilePath);
                    dynamic cachedWorklogs = JsonConvert.DeserializeObject<dynamic>(cachedJson);
                    return cachedWorklogs;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to load worklog cache for {issueKey}, fetching fresh data: {ex.Message}");
                }
            }
            
            // Fetch from Jira API
            int retry = 10;
            while (retry > 0)
            {
                string url = $"{baseUrl}/rest/api/3/issue/{issueKey}/worklog?startAt=0&maxResults=100";
                System.Threading.Thread.Sleep(500);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var response = client.SendAsync(request).Result;

                if (response.IsSuccessStatusCode)
                {
                    string content = response.Content.ReadAsStringAsync().Result;
                    dynamic worklogData = JsonConvert.DeserializeObject<dynamic>(content);
                    
                    // Save to cache file
                    try
                    {
                        string jsonData = JsonConvert.SerializeObject(worklogData, Formatting.Indented);
                        File.WriteAllText(cacheFilePath, jsonData);
                        Console.WriteLine($"💾 Cached worklogs for {issueKey}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Failed to save worklog cache for {issueKey}: {ex.Message}");
                    }
                    
                    return worklogData;
                }
                else
                {
                    if (response.ReasonPhrase == "Too Many Requests")
                    {
                        Console.WriteLine($"Retry jira read {issueKey}");
                        System.Threading.Thread.Sleep(60000);
                        retry--;
                    }
                    else
                    {
                        Console.WriteLine($"Jira read {issueKey} failed: {response.ReasonPhrase}");
                        retry = 0;
                    }

                    Console.WriteLine($"Fout bij ophalen worklogs: {response.StatusCode}");
                }
            }

            return null;
        }

        public static List<dynamic> CollectJiraTickets()
        {
            string cacheDir = @"c:\jira";
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }
            
            string cacheFilePath = Path.Combine(cacheDir, "jira_tickets_cache.json");

            // Check if cache file exists
            if (File.Exists(cacheFilePath))
            {
                try
                {
                    Console.WriteLine($"📂 Loading Jira tickets from cache: {cacheFilePath}");
                    string cachedJson = File.ReadAllText(cacheFilePath);
                    var cachedList = JsonConvert.DeserializeObject<List<dynamic>>(cachedJson);
                    Console.WriteLine($"✔️ Loaded {cachedList.Count} tickets from cache");
                    return cachedList;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to load cache, fetching fresh data: {ex.Message}");
                }
            }

            // Fetch fresh data from Jira
            Console.WriteLine("🌐 Fetching Jira tickets from API...");
            var list = GetJiraTicketsAsync();

            // Save to cache file
            try
            {
                string jsonData = JsonConvert.SerializeObject(list, Formatting.Indented);
                File.WriteAllText(cacheFilePath, jsonData);
                Console.WriteLine($"💾 Cached {list.Count} tickets to: {cacheFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to save cache: {ex.Message}");
            }

            return list;
        }

        public static dynamic JiraIssue(string issueKey)
        {
            // Define cache directory and file path
            string cacheDir = @"c:\jira\issues";
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }
            
            string cacheFilePath = Path.Combine(cacheDir, $"{issueKey}.json");
            
            // Check if cache file exists
            if (File.Exists(cacheFilePath))
            {
                try
                {
                    string cachedJson = File.ReadAllText(cacheFilePath);
                    dynamic cachedIssue = JsonConvert.DeserializeObject<dynamic>(cachedJson);
                    return cachedIssue;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to load cache for {issueKey}, fetching fresh data: {ex.Message}");
                }
            }
            
            // Fetch from Jira API
            bool retry = true;
            dynamic jiraIssue = null;
            while (retry)
            {
                string jiraUrl = $"{baseUrl}/rest/api/3/issue/{issueKey}";
                var response = client.GetAsync(jiraUrl).Result;

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = response.Content.ReadAsStringAsync().Result;
                    jiraIssue = JsonConvert.DeserializeObject<dynamic>(responseBody);
                    retry = false;
                    
                    // Save to cache file
                    try
                    {
                        string jsonData = JsonConvert.SerializeObject(jiraIssue, Formatting.Indented);
                        File.WriteAllText(cacheFilePath, jsonData);
                        Console.WriteLine($"💾 Cached issue {issueKey}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Failed to save cache for {issueKey}: {ex.Message}");
                    }
                }
                else
                {
                    if (response.ReasonPhrase == "Too Many Requests")
                    {
                        Console.WriteLine($"Retry jira read {issueKey}");
                        System.Threading.Thread.Sleep(60000);
                    }
                    else
                    {
                        Console.WriteLine($"Jira read {issueKey} failed: {response.ReasonPhrase}");
                        retry = false;
                    }
                }
            }
            return jiraIssue;
        }

        public static List<dynamic> GetJiraTicketsAsync()
        {
            List<dynamic> allIssues = new List<dynamic>();


            int maxResultsPerCall = 200;
            string nextPageToken = null;
            bool isLast = false;
            Console.WriteLine($"Retrieving tickets for ");

            while (!isLast)
            {
                string jql = $"project in (SP,ES, SOF)";

                // Build URL with nextPageToken if available, otherwise use initial request
                string jiraUrl;
                if (string.IsNullOrEmpty(nextPageToken))
                {
                    jiraUrl = $"{baseUrl}/rest/api/3/search/jql?jql={Uri.EscapeDataString(jql)}&maxResults={maxResultsPerCall}";
                }
                else
                {
                    jiraUrl = $"{baseUrl}/rest/api/3/search/jql?jql={Uri.EscapeDataString(jql)}&maxResults={maxResultsPerCall}&nextPageToken={Uri.EscapeDataString(nextPageToken)}";
                }

                var response = client.GetAsync(jiraUrl).Result;

                if (response.IsSuccessStatusCode)
                {
                    System.Threading.Thread.Sleep(1000);
                    string content = response.Content.ReadAsStringAsync().Result;
                    dynamic searchResult = JsonConvert.DeserializeObject<dynamic>(content);

                    // Add issues to the list
                    foreach (var issue in searchResult.issues)
                    {
                        allIssues.Add(issue);
                    }
                    Console.Write(".");

                    // Check if this is the last page
                    isLast = searchResult.isLast != null ? (bool)searchResult.isLast : true;

                    // Get next page token if available
                    nextPageToken = searchResult.nextPageToken != null ? (string)searchResult.nextPageToken : null;

                    // If isLast is true or no nextPageToken, stop pagination
                    if (isLast || string.IsNullOrEmpty(nextPageToken))
                    {
                        isLast = true;
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Jira API request failed: {(int)response.StatusCode} {response.ReasonPhrase}");
                    string errorBody = response.Content.ReadAsStringAsync().Result;
                    Console.WriteLine($"📦 Error details: {errorBody}");
                    isLast = true; // Stop pagination on error
                }
            }

            Console.WriteLine();
            return allIssues;
        }
    }
}
