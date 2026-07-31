using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NotionTaskSync.Events;
using NotionTaskSync.Integration;
using NotionTaskSync.Utils;

namespace NotionTaskSync.Benchmarks
{
    [MemoryDiagnoser]
    public class WebhookHandlerBenchmarks
    {
        private WebhookHandler _handler = null!;
        private Dictionary<string, object> _sampleData = null!;
        private string _payload = null!;
        private string _secret = null!;
        private string _signature = null!;

        [GlobalSetup]
        public void Setup()
        {
            // Minimal logger – NullLogger is sufficient for benchmarks
            ILogger<WebhookHandler> logger = NullLogger<WebhookHandler>.Instance;

            // EventBus with a simple logger (real implementation is not needed for the benchmark)
            var eventBus = new EventBus(new LoggerFactory().CreateLogger<EventBus>());

            _handler = new WebhookHandler(eventBus, logger);

            // Sample webhook data used by HandleWebhookAsync
            _sampleData = new Dictionary<string, object>
            {
                { "page_id", Guid.NewGuid().ToString() },
                { "database_id", Guid.NewGuid().ToString() },
                { "some_property", "value" }
            };

            // Data for signature validation benchmark
            _payload = "{\"event\":\"test\"}";
            _secret = "super-secret-key";
            _signature = CryptoHelper.ComputeHmacSha256(_payload, _secret);
        }

        // --------------------------------------------------------------------
        // Benchmark: handling a webhook (the most common public async method)
        // --------------------------------------------------------------------
        [Benchmark]
        [Params(10, 100, 1000)]
        public void Benchmark_HandleWebhookAsync(int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Synchronously wait for the async method to keep the benchmark simple
                _handler.HandleWebhookAsync("page_updated", _sampleData).GetAwaiter().GetResult();
            }
        }

        // --------------------------------------------------------------
        // Benchmark: registering many custom handlers (RegisterHandler)
        // --------------------------------------------------------------
        [Benchmark]
        [Params(10, 100, 1000)]
        public void Benchmark_RegisterHandler(int count)
        {
            // Create a fresh handler for each benchmark run to avoid side‑effects
            var freshHandler = new WebhookHandler(
                new EventBus(new LoggerFactory().CreateLogger<EventBus>()),
                NullLogger<WebhookHandler>.Instance);

            for (int i = 0; i < count; i++)
            {
                freshHandler.RegisterHandler($"custom_type_{i}", data => Task.CompletedTask);
            }
        }

        // --------------------------------------------------------------
        // Benchmark: retrieving the list of registered webhook types
        // --------------------------------------------------------------
        [Benchmark]
        public void Benchmark_GetRegisteredWebhookTypes()
        {
            var types = _handler.GetRegisteredWebhookTypes();
            // Prevent the compiler from optimizing away the call
            GC.KeepAlive(types);
        }

        // --------------------------------------------------------------
        // Benchmark: validating a webhook signature (ValidateWebhookSignature)
        // --------------------------------------------------------------
        [Benchmark]
        public void Benchmark_ValidateWebhookSignature()
        {
            var isValid = _handler.ValidateWebhookSignature(_payload, _signature, _secret);
            // Prevent the compiler from optimizing away the result
            GC.KeepAlive(isValid);
        }
    }
}
