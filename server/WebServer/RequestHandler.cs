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

// see: https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/http-message-handlers
public class RequestHandler : DelegatingHandler
{
    private int vistitors = 0;
    // Simple in-memory token store (valid tokens)
    private static HashSet<string> validTokens = new HashSet<string>();
    private static readonly string ADMIN_USERNAME = "pgad";
    private static readonly string ADMIN_PASSWORD = "JezusIsKoning!"; // Change this!
    public static JiraManager jiraManager = new JiraManager();

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

                        DrawGraph(graphics, originalImage);
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

    private void DrawGraph(Graphics graphics, Image originalImage)
    {
        int width = originalImage.Width;
        int height = originalImage.Height;

        graphics.DrawImage(originalImage, 0, 0, originalImage.Width, originalImage.Height);


        using (var font = new Font("Arial", 8, FontStyle.Regular))
        using (var blueBrush = new SolidBrush(Color.FromArgb(255, 0, 0, 255)))
        using (var grayBrush = new SolidBrush(Color.Gray))
        {
            int x0 = 120;
            int y0 = 62;

            //graphics.DrawString(overlayText, font, shadowBrush, x + 2, y + 12);

            int wtot = 970;
            int htot = 235;
            int months = 12;
            int wbar = wtot / months;

            for (int month = 0; month < months; month++)
            {
                // draw bar with 3D effect
                int x = x0 + (month * wbar);

                int ymax = (months - 1);
                int y = ((month / 2) + 6);
                int h = (htot * y) / ymax;
                RectangleF blueRect = new RectangleF(x + 4, y0 + htot - h, wbar - 8, h);
                
                // Create gradient for 3D effect
                using (var gradientBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    blueRect,
                    Color.FromArgb(255, 100, 150, 255), // Lighter blue
                    Color.FromArgb(255, 0, 0, 200),      // Darker blue
                    System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                {
                    graphics.FillRectangle(gradientBrush, blueRect);
                }
                
                // Add highlight on left edge for 3D effect
                using (var highlightPen = new Pen(Color.FromArgb(180, 150, 180, 255), 2))
                {
                    graphics.DrawLine(highlightPen, x + 5, y0 + htot - h, x + 5, y0 + htot);
                }
                
                // Add shadow on right edge for 3D effect
                using (var shadowPen = new Pen(Color.FromArgb(150, 0, 0, 100), 2))
                {
                    graphics.DrawLine(shadowPen, x + wbar - 5, y0 + htot - h, x + wbar - 5, y0 + htot);
                }


                //draw month text
                DateTime date = DateTime.Now;
                date = new DateTime(date.Year, month + 1, date.Day);
                string monthText = date.ToString("MMMM");
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                Rectangle ClientRectangle = new Rectangle(x, y0 + htot + 5, wbar, 30);
                graphics.DrawString(monthText, font, blueBrush, ClientRectangle, sf);

                // Draw value text on top of bar
                int barValue = (htot * ((month / 2) + 1)) / months;
                StringFormat sfTop = new StringFormat();
                sfTop.Alignment = StringAlignment.Center;
                Rectangle topRect = new Rectangle(x, y0 + htot - h - 20, wbar, 20);
                using (var whiteBrush = new SolidBrush(Color.White))
                {
                    graphics.DrawString(barValue.ToString(), font, whiteBrush, topRect, sfTop);
                }

               
            }

            DrawGauge(graphics, 210, 630, 160, 145.35, 250, "Mechanical hoursXX", 45);
            DrawGauge(graphics,415, 630, 160, 145.35, 250, "Software hours", 45);
            DrawGauge(graphics, 803, 630, 160, 145.35, 250, "Visit hours", 45);


            // draw big rectangle box on the bars and year text beneath
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.Gray, 1))
            {
                Rectangle r = new Rectangle(112, 31, 988, 290);
                graphics.DrawRectangle(pen, r);

                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                Rectangle ClientRectangle = new Rectangle(r.Left, r.Bottom+5, r.Width, 30);
                int year = 2027;
                graphics.DrawString($"{year}", font, blueBrush, ClientRectangle, sf);
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

   
}

