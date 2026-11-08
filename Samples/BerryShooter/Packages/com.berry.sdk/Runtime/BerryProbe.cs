using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BerrySdk
{
    public static class BerryProbe
    {
        public static async Task<BerryLauncher> RunAsync(string gameHint = "", int timeoutMs = 1500, Func<string, string> getEnv = null, Func<string, string> readFile = null)
        {
            if (getEnv == null) getEnv = Environment.GetEnvironmentVariable;
            if (readFile == null) readFile = SafeRead;
            int port = ReadPort(getEnv, readFile);
            if (port == 0) return new BerryLauncher { Open = false };
            var client = new BerryClient();
            try
            {
                var payload = await client.ProbeAsync(port, gameHint, timeoutMs).ConfigureAwait(false);
                var l = BerryLauncher.FromJson(payload);
                l.Open = true;
                return l;
            }
            catch (Exception)
            {
                return new BerryLauncher { Open = false };
            }
            finally
            {
                client.Dispose();
            }
        }

        public static int ReadPort(Func<string, string> getEnv, Func<string, string> readFile)
        {
            string text = readFile(Path.Combine(BerrySession.ConfigDir(getEnv), "sdk", "launcher.json"));
            if (string.IsNullOrEmpty(text) || text.Length > 4096) return 0;
            object parsed;
            if (!BerryJson.TryParse(text, out parsed)) return 0;
            int port = (int)BerryJson.Num(parsed, "port");
            return port >= 1024 && port < 65536 ? port : 0;
        }

        private static string SafeRead(string path)
        {
            try { return File.Exists(path) ? File.ReadAllText(path) : null; }
            catch (Exception) { return null; }
        }
    }
}
