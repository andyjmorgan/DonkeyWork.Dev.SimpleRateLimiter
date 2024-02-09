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
                HttpRequestMessage requestMessage = new(HttpMethod.Get, "/todos/1");
                var response = await this.httpClient.SendAsync(requestMessage, cancellationToken);
            });

            stopWatch.Stop();

            Console.WriteLine($"Sent {requestCount} requests in {stopWatch.Elapsed.TotalMilliseconds}");
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
                                                                            retryAttempt)));
        }

        private static PolicyHttpMessageHandler GetPolicyHttpMessageHandler()
        {
            var retryHandler = GetRetryPolicy();
            PolicyHttpMessageHandler policyHttpMessageHandler = new(retryHandler)
            {
                InnerHandler = new HttpClientHandler()
            };
            return policyHttpMessageHandler;
        }

        private static SimpleRateLimitHandler GetSimpleRateLimitHandler()
        {
            var rateLimitHandler = new SimpleRateLimitHandler(requestsPerSecond: Convert.ToInt32(Properties.Resources.RateLimit))
            {
                InnerHandler = GetPolicyHttpMessageHandler()
            };
            return rateLimitHandler;
        }

        private static HttpClient GetHttpClient()
        {
            return new HttpClient(GetSimpleRateLimitHandler())
            {
                BaseAddress = new Uri(Properties.Resources.BaseAddress)
            };
        }
    }
}
