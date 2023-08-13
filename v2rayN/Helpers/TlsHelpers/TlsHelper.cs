using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using v2rayN.Helpers.TlsHelpers.Models;

namespace v2rayN.Helpers.TlsHelpers
{
    public static class TlsHelper
    {
        private static Random rnd = new Random();
        public static async Task<bool> DomainIsTlsOne13(string domain)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Utils.getOpenSSLPath("openssl.exe"),
                Arguments = $"s_client -connect {domain}:443",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

               using var process = Process.Start(startInfo);
            if (process == null)
            {
                Console.WriteLine("Failed to start process");
                return false;
            }

            //await Task.Delay(3000);
            await process.StandardInput.WriteLineAsync("Q");
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();


            return output.Contains("TLSv1.3");
        }



    }
}