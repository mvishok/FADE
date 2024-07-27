using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ShellProgressBar;

namespace FADE
{
    internal class Downloader
    {
        public static async Task<bool> DownloadFileWithProgress(string url, string savePath)
        {
            using var httpClient = new HttpClient();
            using var progress = new ProgressBar(100, "Downloading", new ProgressBarOptions
            {
                ForegroundColor = ConsoleColor.Yellow,
                BackgroundColor = ConsoleColor.DarkYellow,
                ProgressCharacter = '─',
                ProgressBarOnBottom = true
            });

            try
            {
                var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var downloadedBytes = 0L;

                using (var fileStream = new FileStream(savePath, FileMode.Create))
                {
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        var buffer = new byte[4096];
                        int bytesRead;
                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;
                            progress.Tick($"Downloaded: {FormatBytes(downloadedBytes)} / {FormatBytes(totalBytes)}" + " " + (int)Math.Floor(((double)downloadedBytes / totalBytes)*100) + "%");
                        }
                    }
                }

                Console.WriteLine("Download completed successfully!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file: {ex.Message}");
                return false;
            }
        }

        private static string FormatBytes(long bytes)
        {
            const string B = "B";
            const string KB = "KB";
            const string MB = "MB";
            const string GB = "GB";
            const string TB = "TB";

            string[] suffixes = { B, KB, MB, GB, TB };

            double fileSize = bytes;
            int i = 0;
            while (fileSize >= 1024 && i < suffixes.Length)
            {
                fileSize /= 1024;
                ++i;
            }
            return $"{fileSize:F1}{suffixes[i]}";
        }
    }
}