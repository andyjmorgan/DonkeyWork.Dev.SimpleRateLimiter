using DonkeyWork.Dev.SimpleRateLimiter.Example.Classic.Service;
using Moq;
using System.Security.Cryptography.X509Certificates;

namespace DonkeyWork.Dev.SimpleRateLimiter.Tests
{
    public class ConsoleExampleTests
    {
        [Fact]
        public async Task PerformRequests_CompletesSuccessfully()
        {
            // Arrange
            var mockHttpClient = GetMockHttpClient();
            ConsoleRateLimitTester handler = new ConsoleRateLimitTester(mockHttpClient.Object);

            // Act
            await handler.PerformRequestsAsync(new CancellationToken());

            // Assert
            mockHttpClient.Verify(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce());
        }

        [Fact]
        public async Task PerformRequests_WithHttpException_ThrowsException()
        {
            // Arrange
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(x =>
               x.SendAsync(
                   It.IsAny<HttpRequestMessage>(),
                   It.IsAny<CancellationToken>()))
               .Throws<HttpRequestException>();

            ConsoleRateLimitTester handler = new ConsoleRateLimitTester(mockHttpClient.Object);

            // Act
            await Assert.ThrowsAsync<HttpRequestException>(async () => await handler.PerformRequestsAsync(new CancellationToken()));

            // Assert
            mockHttpClient.Verify(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce());
        }

        /// <summary>
        /// Creates a Mock <see cref="HttpClient"/> with a default 200 response.
        /// </summary>
        /// <returns>a <see cref="Mock"/> of <see cref="HttpClient"/>.</returns>
        private static Mock<HttpClient> GetMockHttpClient()
        {
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(x =>
               x.SendAsync(
                   It.IsAny<HttpRequestMessage>(),
                   It.IsAny<CancellationToken>()))
               .ReturnsAsync(
                   new HttpResponseMessage
                   {
                       StatusCode = System.Net.HttpStatusCode.OK
                   });

            return mockHttpClient;
        }
    }
}
