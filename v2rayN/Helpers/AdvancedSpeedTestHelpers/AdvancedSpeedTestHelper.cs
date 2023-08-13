using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net;
using v2rayN.Helpers.RandomFileGeneratorContainer;

namespace v2rayN.Helpers.AdvancedSpeedTestHelpers
{
    public static class AdvancedSpeedTestHelper
    {

        public static class UploadTest
        {
            private static bool IsStarted = false;
            private static Stopwatch stopwatch;

            public static async Task<Speed> Run(Action<string> log)
            {
                var filePath = Utils.GetPath("output.zip");
                if (!File.Exists(filePath))
                    RandomFileGenerator.GenerateRandomFile();
                var url = "http://bouygues.testdebit.info/ul/";
                var timeout = TimeSpan.FromSeconds(10);

                using var cts = new CancellationTokenSource(timeout);
                var proxy = new WebProxy
                {
                    Address = new Uri("socks5://localhost:10808")
                };
                var handler = new HttpClientHandler
                {
                    Proxy = proxy
                };
                using var client = new HttpClient(handler);
                using var content = new MultipartFormDataContent();
                await using var fileStream = File.OpenRead(filePath);
                var progressContent = new ProgressStreamContent(fileStream, TimeSpan.FromSeconds(1), cts, log);
                content.Add(progressContent, "file", Path.GetFileName(filePath));

                stopwatch = Stopwatch.StartNew();

                try
                {
                    var response = await client.PostAsync(url, content, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    log("Upload canceled due to timeout.");
                }

                stopwatch.Stop();
                IsStarted = false;
                var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                var uploadedBytes = progressContent.Position;
                var speedInBytesPerSecond = uploadedBytes / elapsedSeconds;

                //if (File.Exists(Utils.GetPath("output.zip")))
                //    File.Delete(Utils.GetPath("output.zip"));
                //if (File.Exists(Utils.GetPath("random.bin")))
                //    File.Delete(Utils.GetPath("random.bin"));


                return new Speed(speedInBytesPerSecond);
            }

            private class ProgressStreamContent : StreamContent
            {
                private readonly Stream _stream;
                private readonly TimeSpan _interval;
                private readonly CancellationTokenSource _cancellationTokenSource;
                private readonly Action<string> _log;
                public long Position;
                private long _lastPosition;
                private DateTime _lastReportTime;

                public ProgressStreamContent(Stream stream, TimeSpan interval, CancellationTokenSource cancellationTokenSource, Action<string> log)
                    : base(stream)
                {
                    _stream = stream;
                    _interval = interval;
                    _cancellationTokenSource = cancellationTokenSource;
                    _log = log;
                    _lastReportTime = DateTime.Now;
                }

                protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
                {
                    var buffer = new byte[4096];
                    int bytesRead;

                    while ((bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                    {
                        await stream.WriteAsync(buffer, 0, bytesRead);
                        ReportProgress(bytesRead, _log);
                    }
                }

                private void ReportProgress(long bytesRead, Action<string> log)
                {
                    Position += bytesRead;
                    if (IsStarted == false)
                    {
                        stopwatch.Restart();
                        IsStarted = true;
                        _cancellationTokenSource.TryReset();
                    }
                    if (DateTime.Now - _lastReportTime >= _interval)
                    {
                        var bytesUploaded = Position - _lastPosition;
                        log($"Uploaded {bytesUploaded} bytes.");

                        _lastPosition = Position;
                        _lastReportTime = DateTime.Now;
                    }
                }
            }
        }

        public class DownloadTest
        {
            private static bool IsStarted = false;
            public static async Task<Speed> Run(Action<string> log)
            {
                var url = "http://cachefly.cachefly.net/100mb.test";
                var timeout = TimeSpan.FromSeconds(10);

                using var cts = new CancellationTokenSource(timeout);
                var proxy = new WebProxy
                {
                    Address = new Uri("socks5://localhost:10808")
                };
                var handler = new HttpClientHandler
                {
                    Proxy = proxy
                };
                using var client = new HttpClient(handler);
                var stopwatch = Stopwatch.StartNew();
                long totalBytesRead = 0;
                double speedInBytesPerSecond;

                try
                {
                    var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        var totalBytes = response.Content.Headers.ContentLength;
                        using var fileStream = File.Create(Utils.GetPath("file.bin"));
                        using var downloadStream = await response.Content.ReadAsStreamAsync();
                        var buffer = new byte[8192];
                        int bytesRead;
                        var lastReportTime = DateTime.Now;
                        while ((bytesRead = await downloadStream.ReadAsync(buffer, 0, buffer.Length, cts.Token)) != 0)
                        {
                            if (IsStarted == false)
                            {
                                stopwatch.Restart();
                                IsStarted = true;
                            }

                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;
                            if (DateTime.Now - lastReportTime >= TimeSpan.FromSeconds(1))
                            {
                                lastReportTime = DateTime.Now;
                                var progress = (double)totalBytesRead / totalBytes * 100;
                                log($"Downloaded {totalBytesRead} of {totalBytes} bytes. {progress}% complete.");
                            }

                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    log("Download canceled due to timeout.");
                }
                finally
                {
                    stopwatch.Stop();
                    IsStarted = false;
                    var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                    speedInBytesPerSecond = totalBytesRead / elapsedSeconds;
                    if (File.Exists(Utils.GetPath("file.bin")))
                        File.Delete(Utils.GetPath("file.bin"));
                }
                return new Speed(speedInBytesPerSecond);

            }
        }
        public class Speed
        {
            public double BytesPerSecond { get; }
            public double KilobytesPerSecond => BytesPerSecond / 1024;
            public double MegabytesPerSecond => KilobytesPerSecond / 1024;
            public double GigabytesPerSecond => MegabytesPerSecond / 1024;

            public double BitsPerSecond => BytesPerSecond * 8;
            public double KilobitsPerSecond => BitsPerSecond / 1024;
            public double MegabitsPerSecond => KilobitsPerSecond / 1024;
            public double GigabitsPerSecond => MegabitsPerSecond / 1024;

            public Speed(double bytesPerSecond)
            {
                BytesPerSecond = bytesPerSecond;
            }
        }

    }
}