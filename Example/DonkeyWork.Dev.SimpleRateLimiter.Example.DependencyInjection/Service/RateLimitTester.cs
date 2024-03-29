using DonkeyWork.Dev.SimpleRateLimiter.Sample.Interface;
using System.Diagnostics;

namespace DonkeyWork.Dev.SimpleRateLimiter.Sample.Service
{
    /// <summary>
    /// A simple implementation of the <see cref="IRateLimitTester"/> Interface.
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

            var endpointAddress = _configuration.GetValue<string>("EndpointAddress");

            ArgumentException.ThrowIfNullOrWhiteSpace(endpointAddress);
            if (options.MaxDegreeOfParallelism <= 0)
            {
                throw new ArgumentException("Max Degree of Parallelism must be greater than zero");
            }

            if (totalQueries <= 0)
            {
                throw new ArgumentException("Total Queries must be greater than zero");
            }
            
            var requestCount = 0;
            var stopWatch = Stopwatch.StartNew();

            await Parallel.ForEachAsync(Enumerable.Range(0, totalQueries).ToList(), options, async (url, token) =>
            {
                requestCount++;
                using HttpRequestMessage requestMessage = new (HttpMethod.Get, endpointAddress);
                await httpClient
                .SendAsync(requestMessage, cancellationToken)
                .ConfigureAwait(false);
            });

            stopWatch.Stop();
            _logger.LogInformation("Sent {requestCount} requests in {timeTakenMs}", requestCount, stopWatch.ElapsedMilliseconds);
        }
    }
}
