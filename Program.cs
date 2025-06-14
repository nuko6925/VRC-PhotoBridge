using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text.Json;

namespace PhotoBridge;

[SuppressMessage("Interoperability", "CA1416:プラットフォームの互換性を検証")]
[SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
internal static class Program
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(60) };
    private static string _watchDirectory = "watch";
    
    private static readonly DateTime First = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);

    private static long CurrentTimeMillis()
    {
        return (long)(DateTime.Now - First).TotalMilliseconds;
    }

    private static void Main()
    {
        Console.WriteLine("======================= VRC PhotoBridge [1.1] =======================");
        if (Directory.Exists($@"C:\Users\{Environment.UserName}\Pictures\VRChat"))
        {
            _watchDirectory = $@"C:\Users\{Environment.UserName}\Pictures\VRChat";
        }
        else
        {
            while (!Directory.Exists(_watchDirectory))
            {
                Console.WriteLine("Where is the VRChat Photo Directory?");
                Console.WriteLine("Please enter Full Path.");
                var path = Console.ReadLine();
                if (Directory.Exists(path))
                {
                    _watchDirectory = path;
                    Console.WriteLine("Thanks!");
                }
                else
                {
                    Console.WriteLine($"Path '{path}' incorrect.");
                }
            }
        }

        var watcher = new FileSystemWatcher();
        watcher.Path = _watchDirectory;
        watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
        watcher.Filter = "*.png";
        watcher.IncludeSubdirectories = true;
        watcher.Created += FileCreated;
        watcher.EnableRaisingEvents = true;

        Console.WriteLine($"Monitoring directory: {_watchDirectory}");
        Console.WriteLine("Press any key to exit...");

        Console.ReadKey();
    }

    private static async void FileCreated(object obj, FileSystemEventArgs e)
    {
        try
        {
            Console.WriteLine($"[{DateTime.Now:yyyy/MM/dd HH:mm:ss}] File {e.Name} has created.");
            if (!$"{e.Name}".Contains("png")) return;
            var workPath = e.FullPath;
            var fileBytes = await WaitAndReadFileAsync(workPath, retries: 5, delayMs: 500);
            if (fileBytes == null)
            {
                Console.WriteLine($"Failed to read file: {workPath}");
                return;
            }

            var image = Image.FromStream(new MemoryStream(fileBytes));
            if (image.Width > 2048 || image.Height > 2048)
            {
                int newWidth, newHeight;
                if (image.Width > image.Height)
                {
                    newWidth = 2048;
                    newHeight = (int)(image.Height * ((float)2048 / image.Width));
                }
                else
                {
                    newHeight = 2048;
                    newWidth = (int)(image.Width * ((float)2048 / image.Height));
                }

                using var resizedImage = new Bitmap(newWidth, newHeight);
                using var graphics = Graphics.FromImage(resizedImage);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
                if (!Directory.Exists("PhotoBridge-Resize"))
                {
                    Directory.CreateDirectory("PhotoBridge-Resize");
                }

                workPath = Path.Combine(Environment.CurrentDirectory, $"PhotoBridge-Resize\\{CurrentTimeMillis()}.jpg");
                resizedImage.Save(workPath, ImageFormat.Jpeg);
                await Task.Delay(1000);
            }

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.imgur.com/3/image");
            request.Headers.Add("Authorization", "Client-ID xxxxxxxxxxxxxxx");
            using var content = new MultipartFormDataContent();
            await using var fileStream = new FileStream(workPath, FileMode.Open, FileAccess.Read, FileShare.Read); // Use Read-only access
            using var streamContent = new StreamContent(fileStream);
            content.Add(streamContent, "image", Path.GetFileName(workPath));
            content.Add(new StringContent("image"), "type");
            content.Add(new StringContent("VRC-PB"), "title");
            content.Add(new StringContent("Uploaded by VRC PhotoBridge"), "description");
            request.Content = content;
            using var response = await HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(data);
            var link = doc.RootElement
                .GetProperty("data")
                .GetProperty("link")
                .GetString();
            Console.WriteLine(link);
            if (!string.IsNullOrEmpty(link))
            {
                var success = Clipboard.SetText(link);
                Console.WriteLine(success ? "Link copied to clipboard." : "Failed to copy link to clipboard.");
            }
            if (File.Exists(workPath) && workPath.Contains("PhotoBridge-Resize") && workPath.EndsWith(".jpg"))
            {
                fileStream.Close();
                File.Delete(workPath);
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private static async Task<byte[]?> WaitAndReadFileAsync(string filePath, int retries, int delayMs)
    {
        for (var attempt = 0; attempt <= retries; attempt++)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"File not found: {filePath}");
                    return null;
                }

                await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var fileBytes = new byte[stream.Length];
                await stream.ReadExactlyAsync(fileBytes);
                return fileBytes;
            }
            catch (IOException) when (attempt < retries)
            {
                Console.WriteLine($"File {filePath} is locked, retrying ({attempt + 1}/{retries})...");
                await Task.Delay(delayMs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read {filePath}: {ex.Message}");
                return null;
            }
        }

        Console.WriteLine($"Failed to read {filePath} after {retries} retries.");
        return null;
    }
}
