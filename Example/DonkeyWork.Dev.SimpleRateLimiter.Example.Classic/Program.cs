using DonkeyWork.Dev.SimpleRateLimiter.Example.Classic.Service;

namespace DonkeyWork.Dev.SimpleRateLimiter.Example.Classic
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            ConsoleRateLimitTester consoleRateLimitTester  = new ConsoleRateLimitTester();
            await consoleRateLimitTester.PerformRequestsAsync(new CancellationToken());
        }
    }
}