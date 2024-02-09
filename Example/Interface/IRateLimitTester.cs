namespace DonkeyWork.Dev.SimpleRateLimiter.Sample.Interface
{
    /// <summary>
    /// An example interface for testing the Rate Limiter.
    /// </summary>
    public interface IRateLimitTester
    {
        /// <summary>
        /// Perform a small batch of queries.
        /// </summary>
        /// <returns>A <see cref="Task"/>.</returns>
        public Task PerformRequestsAsync(CancellationToken cancellationToken);
    }
}
