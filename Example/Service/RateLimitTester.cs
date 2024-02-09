using DonkeyWork.Dev.SimpleRateLimiter.Sample.Interface;
using EnsureThat;
using System.Diagnostics;

namespace DonkeyWork.Dev.SimpleRateLimiter.Sample.Service
{
    /// <summary>
    /// A simple implementation of a the RateLimterTester Interface.
    /// </summary>
    public class RateLimitTester : IRateLimitTester
    {
        private readonly ILogger<RateLimitTester> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public RateLimitTester(ILogger<RateLimitTester> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }
        /// <inheritdoc />
        public async Task PerformRequestsAsync(CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient(
                   _configuration.GetValue("HttpClientName", "DonkeyWork")!
           );

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = _configuration.GetValue("MaxDegreeOfParallelism", 2)
            };
            var totalQueries = _configuration.GetValue("TotalQueries", 100);
            
            Ensure.That(options.MaxDegreeOfParallelism, nameof(options.MaxDegreeOfParallelism)).IsGt(0);
            Ensure.That(totalQueries, nameof(totalQueries)).IsGt(0);

            int requestCount = 0;
            var stopWatch = Stopwatch.StartNew();

            await Parallel.ForEachAsync(Enumerable.Range(0, totalQueries).ToList(), options, async (url, token) =>
            {
                requestCount++;
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, "/todos/1");
                var response = await httpClient.SendAsync(requestMessage, cancellationToken);
            });

            stopWatch.Stop();
            _logger.LogInformation($"Sent {requestCount} requests in {stopWatch.Elapsed.TotalMilliseconds}");
        }
    }
}
