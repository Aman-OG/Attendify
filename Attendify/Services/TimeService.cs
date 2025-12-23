using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Attendify.Services
{
    public class TimeService
    {
        private static TimeService _instance;
        private static readonly object _lock = new object();
        
        private DateTime _syncTimeUtc = DateTime.MinValue;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private bool _isSynced = false;

        public static TimeService Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new TimeService();
                    }
                    return _instance;
                }
            }
        }

        private TimeService() { }

        public async Task SyncWithServerAsync()
        {
            if (_isSynced) return;

            // Ensure modern TLS protocols are enabled
            System.Net.ServicePointManager.SecurityProtocol |= 
                System.Net.SecurityProtocolType.Tls12 | 
                System.Net.SecurityProtocolType.Tls11 | 
                System.Net.SecurityProtocolType.Tls;

            string[] sources = { 
                "https://www.google.com", 
                "https://www.microsoft.com", 
                "https://www.cloudflare.com",
                "https://www.apple.com"
            };

            foreach (var source in sources)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(3);
                        var request = new HttpRequestMessage(HttpMethod.Head, source);
                        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                        
                        if (response.Headers.Date.HasValue)
                        {
                            _syncTimeUtc = response.Headers.Date.Value.UtcDateTime;
                            _stopwatch.Restart();
                            _isSynced = true;
                            System.Diagnostics.Debug.WriteLine($"[TimeService] Absolute sync with {source}. Real UTC: {_syncTimeUtc}");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TimeService] Failed to sync with {source}: {ex.Message}");
                }
            }

            // Fallback to internal API
            await SyncWithInternalServerAsync();
        }

        private async Task SyncWithInternalServerAsync()
        {
            try
            {
                var client = HttpClientService.Instance;
                var response = await client.GetAsync("system/time");
                
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<InternalTimeResponse>();
                    if (data != null)
                    {
                        _syncTimeUtc = data.ServerTimeUtc;
                        _stopwatch.Restart();
                        _isSynced = true;
                        System.Diagnostics.Debug.WriteLine($"[TimeService] Synced with internal server. Real UTC: {_syncTimeUtc}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TimeService] Failed to sync with internal server: {ex.Message}");
            }
        }

        public DateTime Now 
        {
            get
            {
                if (!_isSynced) return DateTime.Now;
                
                // Return synchronized time + elapsed time since sync
                // This is completely independent of the system clock after sync
                return _syncTimeUtc.Add(_stopwatch.Elapsed).ToLocalTime();
            }
        }

        private class InternalTimeResponse
        {
            public DateTime ServerTimeUtc { get; set; }
        }
    }
}
