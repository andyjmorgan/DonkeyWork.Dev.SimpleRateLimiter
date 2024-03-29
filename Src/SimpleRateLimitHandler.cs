using System.Collections.Concurrent;

namespace DonkeyWork.Dev.SimpleRateLimiter
{
    /// <summary>
    /// A <see cref="HttpMessageHandler"/> delegate to rate limit client requests.
    /// </summary>
    public class SimpleRateLimitHandler : DelegatingHandler, IDisposable
    {
        // a blocking collection to store the recent calls.
        private readonly ConcurrentQueue<DateTime> recentCallQueue = new ();

        // a semaphore to control multithreaded access to the rate limiter.
        private readonly SemaphoreSlim _semaphoreSlim = new (1);

        // The desired period to rate limit.
        private readonly TimeSpan _rateLimitPeriod;

        // The maximum requests per second permissible.
        private readonly int _requestsPerSecond;

        /// <summary>
        /// Default constructor for the RateLimit handler.
        /// </summary>
        /// <param name="requestsPerSecond">The maximum amount of requests throughout the period. (minimum 1).</param>
        public SimpleRateLimitHandler(int requestsPerSecond)
        {
            if (requestsPerSecond <= 0)
            {
                throw new ArgumentException("requests Per Second must be greater than zero");
            }
            _requestsPerSecond = requestsPerSecond;
            _rateLimitPeriod = TimeSpan.FromSeconds(1);
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        {
            await AwaitAllowanceAsync(cancellationToken);
            return await base.SendAsync(request, cancellationToken);
        }

        private async Task AwaitAllowanceAsync(CancellationToken cancellationToken)
        {
            await this._semaphoreSlim.WaitAsync(cancellationToken);

            DequeueExpiredEntries();
            if (this.recentCallQueue.Count < _requestsPerSecond)
            {
                this.recentCallQueue.Enqueue(DateTime.UtcNow);
                this._semaphoreSlim.Release();
                return;
            }

            this.recentCallQueue.TryDequeue(out DateTime lastEntry);
            var waitTime = lastEntry.Subtract(DateTime.UtcNow.Subtract(_rateLimitPeriod));

            this.recentCallQueue.Enqueue(DateTime.UtcNow.Add(waitTime));
            this._semaphoreSlim.Release();

            if (waitTime.TotalMilliseconds < 0)
            {
                return;
            }

            await Task.Delay(waitTime, cancellationToken);
        }

        /// <summary>
        /// Attempt to remove any tracked queries that are no longer in scope.
        /// </summary>
        private void DequeueExpiredEntries()
        {
            while (this.recentCallQueue.TryPeek(out DateTime lastEntry))
            {
                var last = DateTime.UtcNow.Subtract(_rateLimitPeriod);
                if (lastEntry >= last)
                {
                    break;
                }

                this.recentCallQueue.TryDequeue(out _);
            }
        }
    }
}