using System;
using System.Net.Http;

namespace Attendify.Services
{
    public static class HttpClientService
    {
        private static HttpClient _instance;
        private static readonly object _lock = new object();

        // Centralized configuration
        public const string ApiBaseUrl = "https://localhost:7129/api";

        public static HttpClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            var handler = new HttpClientHandler
                            {
                                ServerCertificateCustomValidationCallback = 
                                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                            };

                            _instance = new HttpClient(handler);
                            _instance.BaseAddress = new Uri(ApiBaseUrl + "/");
                            _instance.DefaultRequestHeaders.Add("Accept", "application/json");
                            _instance.Timeout = TimeSpan.FromSeconds(30);
                        }
                    }
                }
                return _instance;
            }
        }
    }
}
