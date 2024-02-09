using DonkeyWork.Dev.SimpleRateLimiter.Example.Classic.Service;

namespace DonkeyWork.Dev.SimpleRateLimiter.Example.Classic
{
    internal class Program
    {
        static async Task Main()
        {
            ConsoleRateLimitTester consoleRateLimitTester  = new ();
            await consoleRateLimitTester.PerformRequestsAsync(new CancellationToken());
        }
    }
}