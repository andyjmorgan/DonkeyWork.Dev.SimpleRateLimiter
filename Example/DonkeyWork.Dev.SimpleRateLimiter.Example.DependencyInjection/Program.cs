using DonkeyWork.Dev.SimpleRateLimiter.Sample.Interface;
using DonkeyWork.Dev.SimpleRateLimiter.Sample.Service;
using Polly;
using Polly.Extensions.Http;
using System.Diagnostics;

namespace DonkeyWork.Dev.SimpleRateLimiter.Example.DependencyInjection
{
    public class Program
    {
        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(100, retryAttempt => TimeSpan.FromMilliseconds(50),
                                                                            onRetry: (outcome, timeSpan, retryAttempt, context) =>
                                                                            {
                                                                                Debug.WriteLine($"{outcome?.Result?.StatusCode} - {outcome?.Result?.Content.ReadAsStringAsync().GetAwaiter().GetResult()} - {timeSpan} - {retryAttempt}");
                                                                            });
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
                        options.BaseAddress = new Uri(builderContext.Configuration.GetValue<string>("BaseAddress")!);
                    })
                    
                    .SetHandlerLifetime(Timeout.InfiniteTimeSpan)
                    .AddPolicyHandler(GetRetryPolicy())
                    .AddHttpMessageHandler(() =>
                        new SimpleRateLimitHandler(
                            requestsPerSecond: 2));
                    services.AddHostedService<Worker>();
                    services.AddTransient<IRateLimitTester, RateLimitTester>();
                })
                .Build();

            host.Run();
        }
    }
}