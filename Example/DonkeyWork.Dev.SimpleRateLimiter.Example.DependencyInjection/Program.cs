using DonkeyWork.Dev.SimpleRateLimiter.Sample.Interface;
using DonkeyWork.Dev.SimpleRateLimiter.Sample.Service;
using Polly;
using Polly.Extensions.Http;

namespace DonkeyWork.Dev.SimpleRateLimiter.Example.DependencyInjection
{
    public class Program
    {
        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
                                                                            retryAttempt)));
        }
        public static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((builderContext, services) =>
                {
                    services.AddHttpClient(
                        builderContext.Configuration.GetValue<string>("HttpClientName", "DonkeyWork")!)
                    .ConfigureHttpClient(options =>
                    {
                        options.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
                    })
                    .AddHttpMessageHandler(() =>
                        new SimpleRateLimitHandler(
                            requestsPerSecond: 10))
                    .SetHandlerLifetime(Timeout.InfiniteTimeSpan)
                    .AddPolicyHandler(GetRetryPolicy());
                    services.AddHostedService<Worker>();
                    services.AddTransient<IRateLimitTester, RateLimitTester>();
                })
                .Build();

            host.Run();
        }
    }
}