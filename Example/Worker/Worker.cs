using DonkeyWork.Dev.SimpleRateLimiter.Sample.Interface;

namespace DonkeyWork.Dev.SimpleRateLimiter.Sample.Worker
{
    public class Worker : BackgroundService
    {
        private readonly IRateLimitTester rateLimitTester;

        public Worker(IRateLimitTester rateLimitTester)
        {
            this.rateLimitTester = rateLimitTester;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await this.rateLimitTester.PerformRequestsAsync(cancellationToken);
        }
    }
}