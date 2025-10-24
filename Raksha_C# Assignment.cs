using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

public class TimeEntry
{
    public string EmployeeName { get; set; }
    public DateTime StarTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public string EntryNotes { get; set; }
    public DateTime? DeletedOn { get; set; }
}


class Program
{
    static HttpClient client = new HttpClient();

    static async Task Main()
    {
        try
        {
            // Get data from API
            string url = "https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";
            string jsonData = await client.GetStringAsync(url);

            // Parse JSON data
            var timeEntries = JsonSerializer.Deserialize<List<TimeEntry>>(jsonData);

            // Calculate total hours for each employee
            Dictionary<string, double> employeeHours = new Dictionary<string, double>();
            
            foreach (var entry in timeEntries)
            {
                if (!string.IsNullOrEmpty(entry.EmployeeName)) // Has name
                {
                    double hours = (entry.EndTimeUtc - entry.StarTimeUtc).TotalHours;
                    
                    // Only add positive hours (some entries have negative hours due to data issues)
                    if (hours > 0)
                    {
                        if (employeeHours.ContainsKey(entry.EmployeeName))
                        {
                            employeeHours[entry.EmployeeName] += hours;
                        }
                        else
                        {
                            employeeHours[entry.EmployeeName] = hours;
                        }
                    }
                }
            }

           

           
            CreateHtmlTable(employeeHours);
           
            makePieChart(employeeHours);

           
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }

    static void makePieChart(Dictionary<string, double> employeeHours)
    {
        
        var sortedEmployees = employeeHours.OrderByDescending(x => x.Value).ToList();
        
        
        double totalHours = sortedEmployees.Sum(x => x.Value);
        
        
        string[] colors = { 
            "#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", "#FFEAA7", 
            "#DDA0DD", "#98D8C8", "#F7DC6F", "#BB8FCE", "#85C1E9" 
        };
        
        
        string html = CreateCanvasHTML(sortedEmployees, totalHours, colors);
        
       
        File.WriteAllText("pie_chart.html", html);
        
       
        bool pngCreated = covertToPng();
        
        if (pngCreated)
        {
            Console.WriteLine("Pie chart created: EmployeeHoursPieChart.png");
        }
        else
        {
            Console.WriteLine("Pie chart HTML created: pie_chart.html");
            Console.WriteLine("Note: PNG conversion requires additional tools. Open pie_chart.html in browser and take screenshot.");
        }
    }
    

    static void CreateHtmlTable(Dictionary<string, double> employeeHours)
    {
        // Sort employees by total hours (descending order)
        var sortedEmployees = employeeHours.OrderByDescending(x => x.Value).ToList();
        
        string html = "<!DOCTYPE html>\n";
        html += "<html>\n<head><title>Employee Hours Report</title></head>\n";
        html += "<body>\n";
        html += "<h1>Employee Hours Report</h1>\n";
        html += "<table border='1'>\n";
        html += "<tr><th>Employee Name</th><th>Total Hours</th></tr>\n";

        foreach (var emp in sortedEmployees)
        {
            string color = emp.Value < 100 ? " style='background-color: #ffcccc;'" : "";
            html += $"<tr{color}><td>{emp.Key}</td><td>{emp.Value:F2}</td></tr>\n";
        }

        html += "</table>\n</body>\n</html>";

        File.WriteAllText("EmployeeHoursTable.html", html);
        Console.WriteLine("HTML table created: EmployeeHoursTable.html");
    }


    
    static string CreateCanvasHTML(List<KeyValuePair<string, double>> sortedEmployees, double totalHours, string[] colors)
    {
        string html = @"<!DOCTYPE html>
<html>
<head>
    <title>Employee Hours Pie Chart</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        canvas { border: 1px solid #ccc; }
        .legend { margin-top: 20px; }
        .legend-item { margin: 5px 0; }
        .color-box { display: inline-block; width: 20px; height: 20px; margin-right: 10px; vertical-align: middle; }
    </style>
</head>
<body>
    <h1>Employee Hours Distribution</h1>
    <canvas id='pieChart' width='800' height='600'></canvas>
    
    <div class='legend'>
        <h3>Legend:</h3>";
        
        int colorIndex = 0;
        foreach (var emp in sortedEmployees)
        {
            double percentage = (emp.Value / totalHours) * 100;
            html += $@"
        <div class='legend-item'>
            <span class='color-box' style='background-color: {colors[colorIndex % colors.Length]};'></span>
            {emp.Key}: {percentage:F1}% ({emp.Value:F1}h)
        </div>";
            colorIndex++;
        }
        
        html += @"
    </div>
    
    <script>
        const canvas = document.getElementById('pieChart');
        const ctx = canvas.getContext('2d');
        
        const centerX = canvas.width / 2;
        const centerY = canvas.height / 2;
        const radius = 200;
        
        const colors = ['#FF6B6B', '#4ECDC4', '#45B7D1', '#96CEB4', '#FFEAA7', 
                       '#DDA0DD', '#98D8C8', '#F7DC6F', '#BB8FCE', '#85C1E9'];
        
        const data = [";
        
        colorIndex = 0;
        foreach (var emp in sortedEmployees)
        {
            double percentage = emp.Value / totalHours;
            html += $"{{name: '{emp.Key}', value: {emp.Value:F2}, percentage: {percentage:F4}, color: '{colors[colorIndex % colors.Length]}'}}";
            if (colorIndex < sortedEmployees.Count - 1) html += ", ";
            colorIndex++;
        }
        
        html += @"];
        
        // Draw title
        ctx.font = 'bold 16px Arial';
        ctx.fillStyle = '#333';
        ctx.textAlign = 'center';
        ctx.fillText('Employee Hours Distribution', centerX, 30);
        
        // Draw pie chart
        let startAngle = 0;
        
        data.forEach((item, index) => {
            const sliceAngle = item.percentage * 2 * Math.PI;
            
            // Draw slice
            ctx.beginPath();
            ctx.moveTo(centerX, centerY);
            ctx.arc(centerX, centerY, radius, startAngle, startAngle + sliceAngle);
            ctx.closePath();
            ctx.fillStyle = item.color;
            ctx.fill();
            
            // Draw border
            ctx.strokeStyle = '#fff';
            ctx.lineWidth = 2;
            ctx.stroke();
            
            startAngle += sliceAngle;
        });
        
        // Add download button
        const downloadBtn = document.createElement('button');
        downloadBtn.textContent = 'Download PNG';
        downloadBtn.style.marginTop = '20px';
        downloadBtn.style.padding = '10px 20px';
        downloadBtn.style.fontSize = '14px';
        downloadBtn.onclick = function() {
            const link = document.createElement('a');
            link.download = 'EmployeeHoursPieChart.png';
            link.href = canvas.toDataURL();
            link.click();
        };
        document.body.appendChild(downloadBtn);
    </script>
</body>
</html>";
        
        return html;
    }
    
     static bool TryLinuxConversion()
    {
        try
        {
            
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "google-chrome",
                    Arguments = "--headless --screenshot=EmployeeHoursPieChart.png --window-size=800,600 pie_chart.html",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            return true;
        }
        catch
        {
            return false;
        }
    }

    static bool covertToPng()
    {
        try
        {
           
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
               
                return TryWindowsConversion();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
               
                return MacOS();
            }
            else
            {
                
                return TryLinuxConversion();
            }
        }
        catch
        {
            return false;
        }
    }
    
    static bool TryWindowsConversion()
    {
        try
        {
            
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = "-Command \"Add-Type -AssemblyName System.Windows.Forms; [System.Windows.Forms.Application]::EnableVisualStyles(); $web = New-Object System.Windows.Forms.WebBrowser; $web.DocumentCompleted += { $web.Document.Window.Size = New-Object System.Drawing.Size(800, 600); $web.Document.ExecCommand('Print', $false, $null) }; $web.Navigate('file:///' + (Get-Location).Path + '/pie_chart.html')\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    static bool MacOS()
    {
        try
        {
           
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = "-a Safari pie_chart.html",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            return true;
        }
        catch
        {
            return false;
        }
    }
    
   
}


