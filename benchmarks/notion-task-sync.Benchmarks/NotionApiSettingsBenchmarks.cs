using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using NotionTaskSync.Infrastructure.Configuration;

namespace NotionTaskSync.Benchmarks
{
    [MemoryDiagnoser]
    public class NotionApiSettingsBenchmarks
    {
        private NotionApiSettings _settings;

        [GlobalSetup]
        public void Setup()
        {
            _settings = new NotionApiSettings
            {
                ApiKey = "1234567890",
                BaseUrl = "https://api.notion.com/v1",
                ApiVersion = "2022-06-28",
                RequestTimeoutSeconds = 30,
                MaxRetries = 3,
                RetryDelayMs = 1000,
                RateLimitPerMinute = 30,
                RespectRateLimits = true,
                DefaultPageSize = 100,
                MaxPageSize = 100,
                EnableCaching = true,
                CacheDurationMinutes = 5,
                MaxPages = 0,
                DatabaseIds = new List<string> { "123", "456" },
                PropertyMappings = new Dictionary<string, string> { { "key", "value" } },
                IncludedStatuses = new List<string> { "Todo", "InProgress" }
            };
        }

        [Benchmark]
        public void Validate()
        {
            _settings.Validate();
        }

        [Benchmark]
        [Params(10, 100, 1000)]
        public void ValidateIncludedStatuses(int count)
        {
            _settings.IncludedStatuses = Enumerable.Range(0, count).Select(i => $"Status{i}").ToList();
            _settings.Validate();
        }

        [Benchmark]
        public void GetMaskedApiKey()
        {
            _settings.GetMaskedApiKey();
        }
    }
}
