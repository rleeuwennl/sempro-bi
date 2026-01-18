using System;
using System.Threading;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Headers;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using SemproJira;
using System.Drawing;
using Newtonsoft.Json.Linq;

// Customer contract data structure
public class CustomerContractEntry
{
    public int Year { get; set; }
    public double ContractHours { get; set; }
    public string HourType { get; set; }

}

// see: https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/http-message-handlers
public class RequestHandler : DelegatingHandler
{
    private int vistitors = 0;
    // Token store with username mapping
    private static Dictionary<string, string> tokenToUsername = new Dictionary<string, string>();
    private static readonly string UsersFilePath = @"c:\jira\users.json";
    
    // User list loaded from JSON file
    private static Dictionary<string, string> Users = new Dictionary<string, string>();
    
    // Customer contract hours loaded from JSON file
    private static Dictionary<string, Dictionary<int, Dictionary<string, double>>> customerContracts = new Dictionary<string, Dictionary<int,Dictionary<string,double>>>();
    
    public static JiraManager jiraManager = new JiraManager();

    public RequestHandler()
    {
        LoadUsers();
        LoadCustomerContracts();
    }

    /// <summary>
    /// Load users from JSON file
    /// </summary>
    private static void LoadUsers()
    {
        try
        {
            if (File.Exists(UsersFilePath))
            {
                string json = File.ReadAllText(UsersFilePath);
                var jsonObj = JObject.Parse(json);
                var usersObj = jsonObj["users"];
                
                Users.Clear();
                foreach (var user in usersObj)
                {
                    var property = (JProperty)user;
                    Users[property.Name] = property.Value.ToString();
                }
                
                Console.WriteLine($"Loaded {Users.Count} users from {UsersFilePath}");
            }
            else
            {
                Console.WriteLine($"Users file not found: {UsersFilePath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading users: {ex.Message}");
        }
    }


    private static void LoadCustomerContracts()
    {
        customerContracts.Clear();
        foreach(var user in Users)
        {
            LoadCustomerContract(user.Key);
        }
    }

    private static void LoadCustomerContract(string customer)
    {        
        string CustomerFilePath = $"c:\\jira\\contracts\\{customer}.json";

        customerContracts.Add(customer,new Dictionary<int, Dictionary<string, double>>());

        try
        {
            if (File.Exists(CustomerFilePath))
            {
                string json = File.ReadAllText(CustomerFilePath);
                var jsonObj = JObject.Parse(json);
                var customersArray = jsonObj["contracts"];
                
                foreach (var contract in customersArray)
                {
                    var contractEntry = new CustomerContractEntry
                    {
                        Year = contract["Contract_Year"]?.Value<int>() ?? 0,
                        ContractHours = contract["Contract_Hours"]?.Value<double>() ?? 0.0,
                        HourType = contract["Hour_Type"]?.Value<string>() ?? ""
                    };

                    if(!customerContracts[customer].ContainsKey(contractEntry.Year))
                    {
                        customerContracts[customer].Add(contractEntry.Year,new Dictionary<string, double>());
                    }

                    if(!customerContracts[customer][contractEntry.Year].ContainsKey(contractEntry.HourType))
                    {
                        //customerContracts[customer][contractEntry.Year].Add(contractEntry.HourType, new double);
                    }

                    customerContracts[customer][contractEntry.Year][contractEntry.HourType] = contractEntry.ContractHours;
                }
                
                Console.WriteLine($"Loaded {customerContracts.Count} customer contracts from {CustomerFilePath}");
            }
            else
            {
                Console.WriteLine($"Customer file not found: {CustomerFilePath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading customer contracts: {ex.Message}");
        }
    }

    /// <summary>
    /// <summary>
    /// Check if request has valid authorization token (checks both header and query string)
    /// </summary>
    private bool IsAuthorized(HttpRequestMessage request)
    {
        string token = null;
        
        // Check header first
        IEnumerable<string> authHeaders;
        if (request.Headers.TryGetValues("X-Auth-Token", out authHeaders))
        {
            token = authHeaders.FirstOrDefault();
        }
        
        // If not in header, check query string
        if (string.IsNullOrEmpty(token))
        {
            token = request.RequestUri.ParseQueryString()["token"];
        }
        
        return !string.IsNullOrEmpty(token) && tokenToUsername.ContainsKey(token);
    }

    private string GetUsernameFromToken(HttpRequestMessage request)
    {
        string token = null;
        
        // Check header first
        IEnumerable<string> authHeaders;
        if (request.Headers.TryGetValues("X-Auth-Token", out authHeaders))
        {
            token = authHeaders.FirstOrDefault();
        }
        
        // If not in header, check query string
        if (string.IsNullOrEmpty(token))
        {
            token = request.RequestUri.ParseQueryString()["token"];
        }
        
        Console.WriteLine($"GetUsernameFromToken - Token: {token}, TokenExists: {tokenToUsername.ContainsKey(token ?? "")}");
        
        if (!string.IsNullOrEmpty(token) && tokenToUsername.ContainsKey(token))
        {
            string username = tokenToUsername[token];
            Console.WriteLine($"GetUsernameFromToken - Found username: {username}");
            return username;
        }
        
        Console.WriteLine("GetUsernameFromToken - No username found for token");
        return null;
    }

    private HttpResponseMessage GetHtml(string filename)
    {
        if (!System.IO.File.Exists(filename))
        {
            return null;
        }

        string result = System.IO.File.ReadAllText(filename);
        result = result.Replace("[visitors]", string.Format("{0:000000}", vistitors));

        HttpResponseMessage response = new HttpResponseMessage();
        response.Content = new StringContent(result);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
        response.StatusCode = HttpStatusCode.OK;
        return response;
    }

    private async Task<HttpResponseMessage> GetFile(string file, string mime)
    {

        file = @"c:/sempro-bi" + file;

        if (!System.IO.File.Exists(file))
        {
            return null;
        }

        if (Path.GetExtension(file) == ".html")
        {
            return GetHtml(file);
        }

        HttpResponseMessage response = new HttpResponseMessage();
        byte[] buffer = File.ReadAllBytes(file);
        response.Content = new StreamContent(new MemoryStream(buffer));
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(mime);
        response.StatusCode = HttpStatusCode.OK;
        response.Content.Headers.ContentLength = buffer.Length;
        return response;
    }




    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string query = request.RequestUri.Query;

        Console.Write(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " =>");
        Console.Write("Request:" + request.RequestUri.LocalPath);
        var response = ProcessRequest(request);

        Console.WriteLine(response != null ? " [OK]" : " [NOK]");

        if (response != null)
        {
            return response;
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private Task<HttpResponseMessage> HandleRootIndex(HttpRequestMessage request)
    {
        string countersFile = "counters.txt";
        if (System.IO.File.Exists(countersFile))
        {
            string s = System.IO.File.ReadAllText(countersFile);
            vistitors = int.Parse(s) + 1;
            Console.WriteLine($"{vistitors} visitors");
            Console.Beep(2000, 100);
        }

        System.IO.File.WriteAllText(countersFile, vistitors.ToString());

        return GetFile("/index.html", "text/html");
    }

    private Task<HttpResponseMessage> HandleLogout(HttpRequestMessage request)
    {
        IEnumerable<string> authHeaders;
        if (request.Headers.TryGetValues("X-Auth-Token", out authHeaders))
        {
            var token = authHeaders.FirstOrDefault();
            tokenToUsername.Remove(token);
        }
        var response = new HttpResponseMessage();
        response.Content = new StringContent("{\"success\":true}");
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return Task.FromResult(response);
    }

    private Task<HttpResponseMessage> HandleGetUsers(HttpRequestMessage request)
    {
        try
        {
            var userList = Users.Keys.ToList();
            var usersJson = "[\"" + string.Join("\",\"", userList) + "\"]";
            var response = new HttpResponseMessage();
            response.Content = new StringContent(usersJson);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting users: {ex.Message}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }

    private Task<HttpResponseMessage> GenerateWorklogTableLoginPrompt()
    {
        var html = new System.Text.StringBuilder();
        html.AppendLine("<div class='worklog-data-content' style='background: white; padding: 20px; min-height: 600px;'>");
        html.AppendLine("    <div class='login-prompt' style='text-align: center; padding: 100px 20px;'>");
        html.AppendLine("        <h2 style='color: #2c5282; margin-bottom: 20px;'>Authentication Required</h2>");
        html.AppendLine("        <p style='font-size: 18px; color: #666; margin-bottom: 30px;'>Please log in to view the worklog data table.</p>");
        html.AppendLine("        <button class='login-btn' onclick='SimpleAuth.showLoginModal()' style='background: #2c5282; color: white; padding: 15px 40px; border: none; border-radius: 5px; font-size: 16px; cursor: pointer;'>Login</button>");
        html.AppendLine("    </div>");
        html.AppendLine("</div>");

        var response = new HttpResponseMessage();
        response.Content = new StringContent(html.ToString());
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
        return Task.FromResult(response);
    }

    private Task<HttpResponseMessage> GenerateWorklogTableHtml(string selectUsername, string selectedYear, string selectedHourType)
    {
        Console.WriteLine($"GenerateWorklogTableHtml called by user: '{selectUsername}', Year: {selectedYear}, Hour Type: {selectedHourType}");

        try
        {
            // Get worklog records
            List<WorklogRecord> allWorkLogs = new List<WorklogRecord>();
            
            if (jiraManager.organisationWorkLogs.ContainsKey(selectUsername))
            {
                allWorkLogs = jiraManager.organisationWorkLogs[selectUsername];
            }
            
            int.TryParse(selectedYear, out int year);

            // Filter records
            var filteredRecords = allWorkLogs
                .Where(w => w.Organization == selectUsername && 
                           w.WorkLogDate.Year == year &&
                           selectedHourType.Contains(w.HourType))
                .OrderBy(w => w.WorkLogDate)
                .ToList();

            // Calculate totals
            double totalHours = filteredRecords.Sum(w => w.TimeSpent);

            // Generate partial HTML (just the content, no full page structure)
            var html = new System.Text.StringBuilder();
            html.AppendLine("<div class='worklog-data-content' style='background: white; padding: 20px; min-height: 600px;'>");
            html.AppendLine("    <div class='info-section' style='padding: 20px; background: #f0f0f0; margin-bottom: 20px;'>");
            html.AppendLine($"        <h2 style='margin-top: 0; color: #2c5282;'>Worklog Data Table - {selectUsername}</h2>");
            html.AppendLine($"        <p><strong>Year:</strong> {year} | <strong>Hour Types:</strong> {selectedHourType}</p>");
            html.AppendLine($"        <p><strong>Total Records:</strong> {filteredRecords.Count} | <strong>Total Hours:</strong> {totalHours:F2}</p>");
            html.AppendLine("    </div>");
            html.AppendLine("    <div class='worklog-table-container' style='width: 100%; overflow-x: auto; margin-top: 20px;'>");
            
            if (filteredRecords.Count == 0)
            {
                html.AppendLine("        <p style='text-align: center; padding: 40px; font-size: 18px; color: #666;'>No worklog records found for the selected criteria.</p>");
            }
            else
            {
                html.AppendLine("        <table class='worklog-table' id='worklogTable' style='width: 100%; border-collapse: collapse; background: white; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>");
                html.AppendLine("            <thead>");
                html.AppendLine("                <tr>");
                html.AppendLine("                    <th class='sortable' data-column='date' style='padding: 12px; text-align: left; border-bottom: 1px solid #ddd; background-color: #2c5282; color: white; font-weight: bold; cursor: pointer; user-select: none;'>Date <span class='sort-arrow'></span></th>");
                html.AppendLine("                    <th class='sortable' data-column='issuekey' style='padding: 12px; text-align: left; border-bottom: 1px solid #ddd; background-color: #2c5282; color: white; font-weight: bold; cursor: pointer; user-select: none;'>Issue Key <span class='sort-arrow'></span></th>");
                html.AppendLine("                    <th class='sortable' data-column='linkedissue' style='padding: 12px; text-align: left; border-bottom: 1px solid #ddd; background-color: #2c5282; color: white; font-weight: bold; cursor: pointer; user-select: none;'>Linked Issue <span class='sort-arrow'></span></th>");
                html.AppendLine("                    <th class='sortable' data-column='author' style='padding: 12px; text-align: left; border-bottom: 1px solid #ddd; background-color: #2c5282; color: white; font-weight: bold; cursor: pointer; user-select: none;'>Author <span class='sort-arrow'></span></th>");
                html.AppendLine("                    <th class='sortable' data-column='hourtype' style='padding: 12px; text-align: left; border-bottom: 1px solid #ddd; background-color: #2c5282; color: white; font-weight: bold; cursor: pointer; user-select: none;'>Hour Type <span class='sort-arrow'></span></th>");
                html.AppendLine("                    <th class='sortable' data-column='hours' data-type='number' style='padding: 12px; text-align: left; border-bottom: 1px solid #ddd; background-color: #2c5282; color: white; font-weight: bold; cursor: pointer; user-select: none;'>Hours <span class='sort-arrow'></span></th>");
                html.AppendLine("                    <th class='sortable' data-column='classification' style='padding: 12px; text-align: left; border-bottom: 1px solid #ddd; background-color: #2c5282; color: white; font-weight: bold; cursor: pointer; user-select: none;'>Classification <span class='sort-arrow'></span></th>");
                html.AppendLine("                    <th class='sortable' data-column='type' style='padding: 12px; text-align: left; border-bottom: 1px solid #ddd; background-color: #2c5282; color: white; font-weight: bold; cursor: pointer; user-select: none;'>Type <span class='sort-arrow'></span></th>");
                html.AppendLine("                    <th class='sortable' data-column='comment' style='padding: 12px; text-align: left; border-bottom: 1px solid #ddd; background-color: #2c5282; color: white; font-weight: bold; cursor: pointer; user-select: none;'>Comment <span class='sort-arrow'></span></th>");
                html.AppendLine("                </tr>");
                html.AppendLine("            </thead>");
                html.AppendLine("            <tbody>");
                
                foreach (var record in filteredRecords)
                {
                    html.AppendLine("                <tr style='border-bottom: 1px solid #ddd;'>");
                    html.AppendLine($"                    <td style='padding: 12px;'>{record.WorkLogDate:yyyy-MM-dd}</td>");
                    html.AppendLine($"                    <td style='padding: 12px;'>{System.Net.WebUtility.HtmlEncode(record.IssueKey)}</td>");
                    html.AppendLine($"                    <td style='padding: 12px;'>{System.Net.WebUtility.HtmlEncode(record.LinkedIssueKey)}</td>");
                    html.AppendLine($"                    <td style='padding: 12px;'>{System.Net.WebUtility.HtmlEncode(record.Author)}</td>");
                    html.AppendLine($"                    <td style='padding: 12px;'>{System.Net.WebUtility.HtmlEncode(record.HourType)}</td>");
                    html.AppendLine($"                    <td style='padding: 12px;'>{record.TimeSpent:F2}</td>");
                    html.AppendLine($"                    <td style='padding: 12px;'>{System.Net.WebUtility.HtmlEncode(record.Classification)}</td>");
                    html.AppendLine($"                    <td style='padding: 12px;'>{System.Net.WebUtility.HtmlEncode(record.TypeOfTicket)}</td>");
                    html.AppendLine($"                    <td style='padding: 12px;'>{System.Net.WebUtility.HtmlEncode(record.Comment ?? "")}</td>");
                    html.AppendLine("                </tr>");
                }
                
                // Add total row
                html.AppendLine("                <tr style='font-weight: bold; background-color: #e2e8f0;'>");
                html.AppendLine("                    <td colspan='5' style='text-align: right; padding: 12px;'><strong>TOTAL:</strong></td>");
                html.AppendLine($"                    <td style='padding: 12px;'><strong>{totalHours:F2}</strong></td>");
                html.AppendLine("                    <td colspan='3' style='padding: 12px;'></td>");
                html.AppendLine("                </tr>");
                
                html.AppendLine("            </tbody>");
                html.AppendLine("        </table>");
            }
            
            html.AppendLine("    </div>");
            html.AppendLine("</div>");

            var response = new HttpResponseMessage();
            response.Content = new StringContent(html.ToString());
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating worklog table HTML: {ex.Message}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }

    private Task<HttpResponseMessage> HandleAuthorization(HttpRequestMessage request)
    {
        try
        {
            var json = request.Content.ReadAsStringAsync().Result;
            // Simple JSON parsing for username and password
            var userMatch = System.Text.RegularExpressions.Regex.Match(json, "\"username\"\\s*:\\s*\"([^\"]+)\"");
            var passMatch = System.Text.RegularExpressions.Regex.Match(json, "\"password\"\\s*:\\s*\"([^\"]+)\"");

            var username = userMatch.Success ? userMatch.Groups[1].Value : "";
            var password = passMatch.Success ? passMatch.Groups[1].Value : "";

            if (Users.ContainsKey(username) && Users[username] == password)
            {
                var token = Guid.NewGuid().ToString();
                tokenToUsername[token] = username;
                var response = new HttpResponseMessage();
                response.Content = new StringContent("{\"success\":true,\"token\":\"" + token + "\",\"username\":\"" + username + "\"}");
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                Console.WriteLine("Login successful for: " + username);
                return Task.FromResult(response);
            }
            Console.WriteLine("Login failed - invalid credentials");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Login error: " + ex.Message);
        }
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
    }

    private Task<HttpResponseMessage> RetrieveGraph(string file, string selectUsername, string selectedYear, string selectedHourType)
    {
        Console.WriteLine($"RetrieveGraph called by user: '{selectUsername}', Year: {selectedYear}, Hour Type: {selectedHourType}");

   

        file = @"c:/sempro-bi" + file;

        if (!System.IO.File.Exists(file))
        {
            return Task.FromResult<HttpResponseMessage>(null);
        }

        try
        {
            // Load the original image
            using (var originalImage = System.Drawing.Image.FromFile(file))
            {
                // Create a new bitmap with the same dimensions
                using (var bitmap = new System.Drawing.Bitmap(originalImage.Width, originalImage.Height))
                {
                    using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        // Set high quality rendering
                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                        DrawGraph(graphics, originalImage,selectUsername, selectedYear,selectedHourType);
                    }

                    // Save to memory stream
                    using (var ms = new MemoryStream())
                    {
                        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        byte[] buffer = ms.ToArray();

                        HttpResponseMessage response = new HttpResponseMessage();
                        response.Content = new ByteArrayContent(buffer);
                        response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                        response.StatusCode = HttpStatusCode.OK;
                        response.Content.Headers.ContentLength = buffer.Length;
                        return Task.FromResult(response);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Image modification error: {ex.Message}");
            return Task.FromResult<HttpResponseMessage>(null);
        }
    }

    private void DrawGraph(Graphics graphics, Image originalImage,string selectUsername,string selectedYear,string selectedHourType)
    {

        List<WorklogRecord> worklogRecords = jiraManager.organisationWorkLogs[selectUsername];
        Dictionary<int, double> worklogPerMonth = new Dictionary<int, double>();

        double mechHours = 0.0;
        double softHours = 0.0;
        double visitHours = 0.0;



        // create empty lists for each month
        for (int month = 0; month < 12; month++)
        {
            worklogPerMonth[month] = 0;
        }

        int.TryParse(selectedYear, out int year);
        foreach (WorklogRecord workLogRecord in worklogRecords)
        {
            if (workLogRecord.Organization != selectUsername) continue;
           

            DateTime workLogDateTime = workLogRecord.WorkLogDate;
            if (workLogDateTime.Year != year) continue;

            switch (workLogRecord.HourType)
            {
                case "Mechanical":
                    mechHours += workLogRecord.TimeSpent;
                    break;

                case "Software":
                    softHours += workLogRecord.TimeSpent;
                    break;

                case "Visit":
                    visitHours += workLogRecord.TimeSpent;
                    break;
            }


            if (!selectedHourType.Contains(workLogRecord.HourType)) continue;

            int month = workLogDateTime.Month - 1;
            worklogPerMonth[month] += workLogRecord.TimeSpent;
        }


        // determine max work hours per month
        double maxWorkHoursPerMonth = 0;
        for (int month = 0; month < 12; month++)
        {
            double w = worklogPerMonth[month];
            if (w > maxWorkHoursPerMonth)
            {
                maxWorkHoursPerMonth = w;
            }
        }

        if (maxWorkHoursPerMonth == 0)
        {
            maxWorkHoursPerMonth = 100;
        }

        int ymax = (((int)maxWorkHoursPerMonth / 100) + 1) * 100;



        int width = originalImage.Width;
        int height = originalImage.Height;

        //graphics.DrawImage(originalImage, 0, 0, originalImage.Width, originalImage.Height);
        graphics.Clear(Color.White);


        using (var font = new Font("Arial", 8, FontStyle.Regular))
        using (var blueBrush = new SolidBrush(Color.FromArgb(255, 0, 0, 255)))
        using (var grayBrush = new SolidBrush(Color.Gray))
        {
            int x0 = 120;
            int y0 = 52;
            int wtot = 970;
            int htot = 235;
            int months = 12;
            int wbar = wtot / months;          



            // Draw vertical scale
            DrawVerticalScale(graphics, x0, y0, wtot, htot, ymax, 5, font, grayBrush);

            // Draw bars
            for (int month = 0; month < months; month++)
            {
                int x = x0 + (month * wbar);
                //int y = ((month / 2) + 6);
                int y = (int)(worklogPerMonth[month]+0.5);
                int h = (htot * y) / ymax;
                
                DrawBar(graphics, x, y0, wbar, htot, h, y, ymax, month, font, blueBrush);
            }


            // show year contract
            double maxMechHours=0.0;
            double maxSoftHours=0.0;
            double maxVistHours=0.0;

            var customerContract = customerContracts[selectUsername];
            if (customerContract.ContainsKey(year))
            {
                var yearContract = customerContract[year];
                maxMechHours = yearContract["Mechanical"];
                maxSoftHours = yearContract["Software"];
                maxVistHours = yearContract["Visit"];
            }


            DrawGauge(graphics, 210, 600, 160, mechHours, maxMechHours, "Mechanical hours", 45);
            DrawGauge(graphics,615, 600, 160, softHours, maxSoftHours, "Software hours", 45);
            DrawGauge(graphics, 1003, 600, 160, visitHours, maxVistHours, "Visit hours", 45);


            // draw big rectangle box on the bars and year text beneath
            using (var pen = new Pen(Color.Gray, 1))
            {
                Rectangle r = new Rectangle(112, y0-31, 988, 290);
                graphics.DrawRectangle(pen, r);

                // Draw "Melexis" text above the rectangle (centered)
                StringFormat sfCenter = new StringFormat();
                sfCenter.Alignment = StringAlignment.Center;
                Rectangle titleRect = new Rectangle(r.Left, r.Top - 23, r.Width, 20);
                using (var titleFont = new Font("Arial", 12, FontStyle.Bold))
                {
                    graphics.DrawString(selectUsername, titleFont, blueBrush, titleRect, sfCenter);
                }

                // Draw year text below the rectangle
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                Rectangle ClientRectangle = new Rectangle(r.Left, r.Bottom+5, r.Width, 30);
                using (var yearFont = new Font("Arial", 9, FontStyle.Regular))
                {
                    graphics.DrawString($"{year}", yearFont, blueBrush, ClientRectangle, sf);
                }
            }
        }


       

    }

    private Task<HttpResponseMessage> ProcessRequest(HttpRequestMessage request)
    {
        try
        {
            string line = request.RequestUri.LocalPath;

            string path = Path.GetDirectoryName(line);

            // Handle authorization endpoints
            if (line == "/api/auth/users")
            {
                return HandleGetUsers(request);
            }
            
            if (line == "/api/auth/login")
            {
                return HandleAuthorization(request);
            }

            if (line == "/api/auth/logout")
            {
                return HandleLogout(request);
            }

            // Handle image endpoint (requires authentication)
            if (line == "/api/retrieve/graph")
            {
                if (!IsAuthorized(request))
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
                }

                string username = GetUsernameFromToken(request);
                
                // Auto-logout if username is empty
                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("Username is empty - invalidating token");
                    // Remove token from dictionary
                    string token = null;
                    IEnumerable<string> authHeaders;
                    if (request.Headers.TryGetValues("X-Auth-Token", out authHeaders))
                    {
                        token = authHeaders.FirstOrDefault();
                    }
                    if (string.IsNullOrEmpty(token))
                    {
                        token = request.RequestUri.ParseQueryString()["token"];
                    }
                    if (!string.IsNullOrEmpty(token))
                    {
                        tokenToUsername.Remove(token);
                    }
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
                }
                
                string year = request.RequestUri.ParseQueryString()["year"] ?? "unknown";
                string hourType = request.RequestUri.ParseQueryString()["hourType"] ?? "unknown";
                
                return RetrieveGraph("/images/pic01.jpg", username, year, hourType);
            }

            // Handle worklog table HTML page
            if (line == "/worklog-table.html")
            {
                Console.WriteLine("Worklog table requested");
                
                // Check if user is authenticated
                if (!IsAuthorized(request))
                {
                    Console.WriteLine("User not authorized - showing login prompt");
                    // Show login prompt page
                    return GenerateWorklogTableLoginPrompt();
                }

                Console.WriteLine("User authorized - generating table");
                string username = GetUsernameFromToken(request);
                
                // Auto-logout if username is empty
                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("Username is empty - invalidating token and showing login");
                    // Remove token from dictionary
                    string token = null;
                    IEnumerable<string> authHeaders;
                    if (request.Headers.TryGetValues("X-Auth-Token", out authHeaders))
                    {
                        token = authHeaders.FirstOrDefault();
                    }
                    if (string.IsNullOrEmpty(token))
                    {
                        token = request.RequestUri.ParseQueryString()["token"];
                    }
                    if (!string.IsNullOrEmpty(token))
                    {
                        tokenToUsername.Remove(token);
                    }
                    return GenerateWorklogTableLoginPrompt();
                }
                
                string year = request.RequestUri.ParseQueryString()["year"] ?? DateTime.Now.Year.ToString();
                string hourType = request.RequestUri.ParseQueryString()["hourType"] ?? "Mechanical hours, Software hours, Visit hours";
                
                return GenerateWorklogTableHtml(username, year, hourType);
            }



            // For authorized requests, add to console output
            if (IsAuthorized(request))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(" [AUTHORIZED]");
                Console.ResetColor();
            }

            if (line == "/")
            {
                return HandleRootIndex(request);
            }

            if (line.StartsWith("/.well-known/acme-challenge"))
            {
                return GetFile(line, "text/plain");
            }

            // All .html requests: serve the fragment only when explicitly asked; otherwise serve shell
            if (Path.GetExtension(line) == ".html")
            {
                bool isFragmentRequest = request.Headers.Contains("X-Fragment-Request");

                if (isFragmentRequest)
                {
                    return GetFile(line, "text/html");
                }

                // Serve shell (index.html) so client-side loader can inject fragment
                return GetFile("/index.html", "text/html");
            }

            if (line.StartsWith("/images/") || line.StartsWith("/assets/") || line.StartsWith("/pdf/") || line.StartsWith("/html/") || line.StartsWith("/json/") || line.StartsWith("/liturgie/"))
            {
                string ext = Path.GetExtension(line);

                switch (ext)
                {
                    case ".css": return GetFile(line, "text/css");
                    case ".js": return GetFile(line, "text/jscript");
                    case ".txt": return GetFile(line, "text/html");
                    case ".ico": return GetFile(line, "image/x-icon");
                    case ".jpg": return GetFile(line, "image/jpeg");
                    case ".png": return GetFile(line, "image/png");
                    case ".woff2": return GetFile(line, "font/woff2");
                    case ".pdf": return GetFile(line, "application/pdf");
                    case ".json": return GetFile(line, "application/json");
                }
            }

        }
        catch (Exception e)
        {
            Console.WriteLine($"FAILURE {e.Message} \r\n {e.StackTrace}");
            Console.Beep(500, 1000);
        }

        return null;
    }

    /// <summary>
    /// Draws a beautiful semicircular gauge with gradient effects
    /// </summary>
    /// <param name="graphics">Graphics object to draw on</param>
    /// <param name="x">X position of gauge center</param>
    /// <param name="y">Y position of gauge bottom center</param>
    /// <param name="radius">Radius of the gauge</param>
    /// <param name="value">Current value to display</param>
    /// <param name="maxValue">Maximum value of the gauge</param>
    /// <param name="title">Title text above gauge</param>
    /// <param name="arcThickness">Thickness of the arc</param>
    public void DrawGauge(Graphics graphics, int x, int y, int radius, double value, double maxValue, string title = "Mechanical hours", int arcThickness = 40)
    {
        // Enable anti-aliasing for smooth curves
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        // Calculate percentage
        double percentage = Math.Min(Math.Max(value / maxValue, 0), 1);

        // Define gauge rectangle
        Rectangle gaugeRect = new Rectangle(x - radius, y - radius, radius * 2, radius * 2);
        Rectangle innerRect = new Rectangle(x - radius + arcThickness, y - radius + arcThickness, 
                                           (radius - arcThickness) * 2, (radius - arcThickness) * 2);

        // Draw title
        using (var titleFont = new Font("Segoe UI", 16, FontStyle.Regular))
        using (var titleBrush = new SolidBrush(Color.FromArgb(50, 50, 50)))
        {
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            graphics.DrawString(title, titleFont, titleBrush, x, y - radius - 55, sf);
        }

        // Draw background arc (darker gray)
        using (var bgPen = new Pen(Color.FromArgb(180, 180, 180), arcThickness))
        {
            bgPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            bgPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            graphics.DrawArc(bgPen, gaugeRect, 180, 180);
        }

        // Draw value arc with gradient effect
        if (percentage > 0)
        {
            // Always use green color
            Color startColor = Color.FromArgb(0, 120, 0);
            Color endColor = Color.FromArgb(0, 180, 0);

            using (var valuePen = new Pen(startColor, arcThickness))
            {
                valuePen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                valuePen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                
                // Create gradient brush for the arc
                using (var gradientBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    gaugeRect,
                    startColor,
                    endColor,
                    System.Drawing.Drawing2D.LinearGradientMode.ForwardDiagonal))
                {
                    valuePen.Brush = gradientBrush;
                    float sweepAngle = (float)(180 * percentage);
                    graphics.DrawArc(valuePen, gaugeRect, 180, sweepAngle);
                }
            }

            // Add inner shadow effect
            using (var shadowPen = new Pen(Color.FromArgb(40, 0, 0, 0), arcThickness - 4))
            {
                shadowPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                shadowPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                float sweepAngle = (float)(180 * percentage);
                Rectangle shadowRect = new Rectangle(gaugeRect.X + 2, gaugeRect.Y + 2, gaugeRect.Width, gaugeRect.Height);
                graphics.DrawArc(shadowPen, shadowRect, 180, sweepAngle);
            }
        }

        // Draw center value
        using (var valueFont = new Font("Segoe UI Light", 36, FontStyle.Regular))
        using (var valueBrush = new SolidBrush(Color.FromArgb(120, 120, 120)))
        {
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            graphics.DrawString(value.ToString("F2"), valueFont, valueBrush, x, y - radius / 3, sf);
        }

        // Draw min value label (bottom left)
        using (var labelFont = new Font("Segoe UI", 12, FontStyle.Regular))
        using (var labelBrush = new SolidBrush(Color.FromArgb(120, 120, 120)))
        {
            graphics.DrawString("1.23", labelFont, labelBrush, x - radius + 5, y + 20);
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Far;
            graphics.DrawString(maxValue.ToString("F0"), labelFont, labelBrush, x + radius - 5, y + 20, sf);
        }

    }

    /// <summary>
    /// Draws a vertical scale with grid lines and vertical axis label
    /// </summary>
    /// <param name="graphics">Graphics object to draw on</param>
    /// <param name="x0">Starting X position</param>
    /// <param name="y0">Starting Y position</param>
    /// <param name="width">Width of the chart area</param>
    /// <param name="height">Height of the chart area</param>
    /// <param name="maxValue">Maximum value for the scale</param>
    /// <param name="steps">Number of scale divisions</param>
    /// <param name="font">Font for scale labels</param>
    /// <param name="brush">Brush for scale labels</param>
    private void DrawVerticalScale(Graphics graphics, int x0, int y0, int width, int height, int maxValue, int steps, Font font, Brush brush)
    {
        // Draw vertical axis label (rotated 90 degrees)
        using (var labelFont = new Font("Arial", 10, FontStyle.Bold))
        {
            graphics.TranslateTransform(x0 - 45, y0 + height / 2);
            graphics.RotateTransform(-90);
            
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            graphics.DrawString("Sum logged hours", labelFont, brush, 0, 0, sf);
            
            graphics.ResetTransform();
        }
        
        for (int i = 0; i <= steps; i++)
        {
            int scaleValue = (maxValue * i) / steps;
            int scaleY = y0 + height - (height * i / steps);
            
            // Draw scale line
            using (var scalePen = new Pen(Color.FromArgb(200, 200, 200), 1))
            {
                scalePen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                graphics.DrawLine(scalePen, x0 - 5, scaleY, x0 + width, scaleY);
            }
            
            // Draw scale text
            StringFormat sfRight = new StringFormat();
            sfRight.Alignment = StringAlignment.Far;
            graphics.DrawString(scaleValue.ToString(), font, brush, x0 - 8, scaleY - 6, sfRight);
        }
    }

    /// <summary>
    /// Draws a single bar with 3D effect, month label, and value text
    /// </summary>
    /// <param name="graphics">Graphics object to draw on</param>
    /// <param name="x">X position of the bar</param>
    /// <param name="y0">Base Y position</param>
    /// <param name="barWidth">Width of the bar</param>
    /// <param name="chartHeight">Total chart height</param>
    /// <param name="barHeight">Height of this bar</param>
    /// <param name="value">Value to display</param>
    /// <param name="maxValue">Maximum value for scaling</param>
    /// <param name="monthIndex">Month index (0-11)</param>
    /// <param name="font">Font for text</param>
    /// <param name="textBrush">Brush for text</param>
    private void DrawBar(Graphics graphics, int x, int y0, int barWidth, int chartHeight, int barHeight, int value, int maxValue, int monthIndex, Font font, Brush textBrush)
    {
        // Handle zero height - draw a thin line instead
        int effectiveHeight = barHeight > 0 ? barHeight : 2;
        
        RectangleF barRect = new RectangleF(x + 4, y0 + chartHeight - effectiveHeight, barWidth - 8, effectiveHeight);
        
        // Create gradient for 3D effect
        using (var gradientBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            barRect,
            Color.FromArgb(255, 100, 150, 255), // Lighter blue
            Color.FromArgb(255, 0, 0, 200),      // Darker blue
            System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
        {
            graphics.FillRectangle(gradientBrush, barRect);
        }
        
        // Add highlight on left edge for 3D effect
        using (var highlightPen = new Pen(Color.FromArgb(180, 150, 180, 255), 2))
        {
            graphics.DrawLine(highlightPen, x + 5, y0 + chartHeight - effectiveHeight, x + 5, y0 + chartHeight);
        }
        
        // Add shadow on right edge for 3D effect
        using (var shadowPen = new Pen(Color.FromArgb(150, 0, 0, 100), 2))
        {
            graphics.DrawLine(shadowPen, x + barWidth - 5, y0 + chartHeight - effectiveHeight, x + barWidth - 5, y0 + chartHeight);
        }

        // Draw month text (abbreviated for better readability)
        DateTime date = DateTime.Now;
        date = new DateTime(date.Year, monthIndex + 1, date.Day);
        string monthText = date.ToString("MMM");  // Use abbreviated month name
        StringFormat sf = new StringFormat();
        sf.Alignment = StringAlignment.Center;
        Rectangle monthRect = new Rectangle(x, y0 + chartHeight + 5, barWidth, 30);
        using (var monthFont = new Font("Arial", 9, FontStyle.Regular))
        {
            graphics.DrawString(monthText, monthFont, textBrush, monthRect, sf);
        }

        // Draw y value text on top of bar (horizontally centered)
        StringFormat sfTop = new StringFormat();
        sfTop.Alignment = StringAlignment.Center;
        sfTop.LineAlignment = StringAlignment.Far;
        Rectangle topRect = new Rectangle(x, y0 + chartHeight - effectiveHeight - 22, barWidth, 20);
        using (var whiteBrush = new SolidBrush(Color.Blue))
        {
            graphics.DrawString(value.ToString(), font, whiteBrush, topRect, sfTop);
        }
    }

   
}

