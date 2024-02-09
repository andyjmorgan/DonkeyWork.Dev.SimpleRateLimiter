using DonkeyWork.Dev.SimpleRateLimiter.Example.Classic.Interface;
using Microsoft.Extensions.Http;
using Polly.Extensions.Http;
using Polly;
using System.Diagnostics;

namespace DonkeyWork.Dev.SimpleRateLimiter.Example.Classic.Service
{
    public class ConsoleRateLimitTester : IConsoleRateLimitTester
    {
        private readonly HttpClient httpClient;
        
        public ConsoleRateLimitTester()
        {
            this.httpClient = GetHttpClient();
        }

        public ConsoleRateLimitTester(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        /// <inheritdoc />
        public async Task PerformRequestsAsync(CancellationToken cancellationToken)
        {
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Convert.ToInt32(Properties.Resources.MaxDegreeOfParallelism)
            };

            int requestCount = Convert.ToInt32(Properties.Resources.TotalQueries);
            var stopWatch = Stopwatch.StartNew();

            await Parallel.ForEachAsync(Enumerable.Range(0, requestCount).ToList(), options, async (url, token) =>
            {
                HttpRequestMessage requestMessage = new(HttpMethod.Get, Properties.Resources.EndpointAddress);
                var response = await this.httpClient.SendAsync(requestMessage, cancellationToken);
                Console.WriteLine(response.StatusCode);
            });

            stopWatch.Stop();

            Console.WriteLine($"Sent {requestCount} requests in {stopWatch.Elapsed.TotalMilliseconds}");
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
              .HandleTransientHttpError()
              .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.InsufficientStorage)
              .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
              .WaitAndRetryAsync(100, retryAttempt => TimeSpan.FromMilliseconds(10 *
                                retryAttempt),
                                onRetry: (outcome, timeSpan, retryAttempt, context) =>
                                {
                                    Debug.WriteLine(
                                        $"{outcome?.Result?.StatusCode} - {outcome?.Result?.Content.ReadAsStringAsync().GetAwaiter().GetResult()} - {timeSpan} - {retryAttempt}");
                                });
        }

        private static PolicyHttpMessageHandler GetPolicyHttpMessageHandler()
        {
            var retryHandler = GetRetryPolicy();
            PolicyHttpMessageHandler policyHttpMessageHandler = new(retryHandler)
            {
                InnerHandler = GetSimpleRateLimitHandler()
            };
            return policyHttpMessageHandler;
        }

        private static SimpleRateLimitHandler GetSimpleRateLimitHandler()
        {
            var rateLimitHandler = new SimpleRateLimitHandler(requestsPerSecond: Convert.ToInt32(Properties.Resources.RateLimit))
            {
                InnerHandler = new HttpClientHandler()
            };
            return rateLimitHandler;
        }

        private static HttpClient GetHttpClient()
        {
            return new HttpClient(GetPolicyHttpMessageHandler())
            {
                BaseAddress = new Uri(Properties.Resources.BaseAddress)
            };
        }
    }
}
