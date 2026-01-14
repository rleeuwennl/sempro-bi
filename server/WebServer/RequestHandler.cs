using System;
using System.Threading;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;
using System.Linq;

// see: https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/http-message-handlers
public class RequestHandler : DelegatingHandler
{
    private int vistitors = 0;
    // Simple in-memory token store (valid tokens)
    private static HashSet<string> validTokens = new HashSet<string>();
    private static readonly string ADMIN_USERNAME = "pgad";
    private static readonly string ADMIN_PASSWORD = "JezusIsKoning!"; // Change this!


    public RequestHandler()
    {

    }

    /// <summary>
    /// Check if request has valid authorization token
    /// </summary>
    private bool IsAuthorized(HttpRequestMessage request)
    {
        IEnumerable<string> authHeaders;
        if (request.Headers.TryGetValues("X-Auth-Token", out authHeaders))
        {
            var token = authHeaders.FirstOrDefault();
            return !string.IsNullOrEmpty(token) && validTokens.Contains(token);
        }
        return false;
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
            validTokens.Remove(token);
        }
        var response = new HttpResponseMessage();
        response.Content = new StringContent("{\"success\":true}");
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return Task.FromResult(response);
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

            if (username == ADMIN_USERNAME && password == ADMIN_PASSWORD)
            {
                var token = Guid.NewGuid().ToString();
                validTokens.Add(token);
                var response = new HttpResponseMessage();
                response.Content = new StringContent("{\"success\":true,\"token\":\"" + token + "\"}");
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

    private Task<HttpResponseMessage> RetrieveGraph(string file)
    {
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

                        // Draw the original image
                        graphics.DrawImage(originalImage, 0, 0, originalImage.Width, originalImage.Height);

                        // Add text overlay
                        string overlayText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        using (var font = new System.Drawing.Font("Arial", 24, System.Drawing.FontStyle.Bold))
                        //using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                        using (var shadowBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(128, 0, 0, 0)))
                        {
                            // Measure text size
                            var textSize = graphics.MeasureString(overlayText, font);
                            float x = 10;
                            float y = 10;

                            // Draw shadow
                            graphics.DrawString(overlayText, font, shadowBrush, x + 2, y + 12);
                            // Draw text
                            //graphics.DrawString(overlayText, font, brush, x, y + 14);
                        }

                        // Draw a rectangle border
                        using (var pen = new System.Drawing.Pen(System.Drawing.Color.Red, 5))
                        {
                            // graphics.DrawRectangle(pen, 5, 5, bitmap.Width - 10, bitmap.Height - 10);
                        }
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
    private Task<HttpResponseMessage> ProcessRequest(HttpRequestMessage request)
    {
        try
        {
            string line = request.RequestUri.LocalPath;

            string path = Path.GetDirectoryName(line);

            // Handle authorization endpoints
            if (line == "/api/auth/login")
            {
                return HandleAuthorization(request);
            }

            if (line == "/api/auth/logout")
            {
                return HandleLogout(request);
            }

            // Handle image endpoint
            if (line == "/api/retrieve/graph")
            {
                return RetrieveGraph("/images/pic01.jpg");
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

            if(line.StartsWith("/.well-known/acme-challenge"))
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
}

