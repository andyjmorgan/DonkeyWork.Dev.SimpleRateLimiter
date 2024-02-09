using DonkeyWork.Dev.SimpleRateLimiter.Sample.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace DonkeyWork.Dev.SimpleRateLimiter.Tests
{
    public class SampleTests
    {
        private const string MockConfigurationHttpClientName = "DonkeyWork";
        private const int MockConfigurationDegreesOfParallelism = 2;
        private const int MockConfigurationTotalQueries = 100;

        /// <summary>
        /// Tests to ensure, when setup correctly, the example application performs the task requested.
        /// </summary>
        /// <returns>a Task.</returns>
        [Fact]
        public async Task PerformRequests_CompletesSuccessfully()
        {
            // Arrange
            var (handler, mockILogger, mockIConfiguration, mockIHttpClientFactory, mockHttpClient) = GetHandler();

            // Act + Assert
            await handler.PerformRequestsAsync(new CancellationToken());
            mockIHttpClientFactory.Verify(x => x.CreateClient(It.Is<string>(x => x == MockConfigurationHttpClientName)), times: Times.Once);
            mockIConfiguration.Verify(x => x.GetSection(It.IsAny<string>()), times: Times.Exactly(3));
            mockHttpClient.Verify(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(MockConfigurationTotalQueries));
            Mock.VerifyAll();
        }

        /// <summary>
        /// Configures the Max Degree of Parallelism with an invalid value and ensures it throws an <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        /// <returns>A Task.</returns>
        [Fact]
        public async Task PerformRequests_WithInvalidParallelism_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var configurationDictionary = new Dictionary<string, string>
            {
                { "HttpClientName", MockConfigurationHttpClientName },
                { "MaxDegreeOfParallelism", 0.ToString() },
                { "TotalQueries",  MockConfigurationTotalQueries.ToString() }
            };

            var (handler, mockILogger, mockIConfiguration, mockIHttpClientFactory, mockHttpClient) 
                = GetHandler(mockIConfiguration: GetConfigurationMock(configurationDictionary));

            // Act + Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await handler.PerformRequestsAsync(new CancellationToken()));
            mockIHttpClientFactory.Verify(x => x.CreateClient(It.Is<string>(x => x == MockConfigurationHttpClientName)), times: Times.Once);
            mockIConfiguration.Verify(x => x.GetSection(It.IsAny<string>()), times: Times.Exactly(2));
            mockHttpClient.Verify(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Configures the Total Queries with an invalid value and ensures it throws an <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        /// <returns>The Task.</returns>
        [Fact]
        public async Task PerformRequests_WithInvalidQueries_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var configurationDictionary = new Dictionary<string, string>
            {
                { "HttpClientName", MockConfigurationHttpClientName },
                { "MaxDegreeOfParallelism", MockConfigurationDegreesOfParallelism.ToString() },
                { "TotalQueries",  0.ToString() }
            };

            var (handler, mockILogger, mockIConfiguration, mockIHttpClientFactory, mockHttpClient) =
                GetHandler(mockIConfiguration: GetConfigurationMock(configurationDictionary));

            // Act + Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await handler.PerformRequestsAsync(new CancellationToken()));
            mockIHttpClientFactory.Verify(x => x.CreateClient(It.Is<string>(x => x == MockConfigurationHttpClientName)), times:  Times.Once);
            mockIConfiguration.Verify(x => x.GetSection(It.IsAny<string>()), times: Times.Exactly(3));
            mockHttpClient.Verify(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Tests to ensure the HttpClient Exception is passed back through the handler.
        /// </summary>
        /// <returns>A Task.</returns>
        [Fact]
        public async Task TaskPerformsRequests_WithHttpException_ThrowsException()
        {
            // Arrange
            Mock<HttpClient> mockHttpClient = new ();
            mockHttpClient.Setup(x =>
            x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())

            ).Throws<HttpRequestException>();

            var mockingSetup = GetHandler(mockHttpClient: mockHttpClient);

            // Act + Assert
            await Assert.ThrowsAsync<HttpRequestException>(async () => await mockingSetup.handler.PerformRequestsAsync(new CancellationToken()));
            mockingSetup.mockIHttpClientFactory.Verify(x => x.CreateClient(It.Is<string>(x => x == MockConfigurationHttpClientName)), times: Times.Once);
            mockingSetup.mockIConfiguration.Verify(x => x.GetSection(It.IsAny<string>()), times: Times.Exactly(3));
            mockingSetup.mockHttpClient.Verify(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        /// <summary>
        /// Creates a <see cref="Mock"/> <see cref="ILogger"/> for use in mocking.
        /// </summary>
        /// <typeparam name="T">The desired type for the ILogger</typeparam>
        /// <returns>A <see cref="Mock"/> <see cref="ILogger"/>.</returns>
        private static Mock<ILogger<T>> GetMockILogger<T>()
        {
            return new Mock<ILogger<T>>();
        }

        /// <summary>
        /// Creates a mock <see cref="IConfigurationSection"/> response and section.
        /// </summary>
        /// <param name="key">The Configuration key name.</param>
        /// <param name="value">The Configuration value.</param>
        /// <returns>A mocked <see cref=" IConfigurationSection"/>.</returns>
        private static Mock<IConfigurationSection> GetConfigurationSection(string key, string value)
        {
            var mockIConfigurationSection = new Mock<IConfigurationSection>();
            mockIConfigurationSection.Setup(x => x.Key).Returns(key);
            mockIConfigurationSection.Setup(x => x.Value).Returns(value);
            return mockIConfigurationSection;
        }

        /// <summary>
        /// Creates a mock <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="configurationDictionary">A configurable configuration dictionary to override the default.</param>
        /// <returns>A mock <see cref="IConfiguration"/>.</returns>
        private static Mock<IConfiguration> GetConfigurationMock(Dictionary<string, string> configurationDictionary)
        {
            if (!configurationDictionary.Any())
            {
                configurationDictionary = new Dictionary<string, string>
            {
                { "HttpClientName", MockConfigurationHttpClientName },
                { "MaxDegreeOfParallelism", MockConfigurationDegreesOfParallelism.ToString() },
                { "TotalQueries",  MockConfigurationTotalQueries.ToString() }
            };
            }
            var configurationMock = new Mock<IConfiguration>();
            var mockIConfigurationSection = new Mock<IConfigurationSection>();

            foreach(var configurationItem in configurationDictionary)
            {
                configurationMock.SetupGet(x => x[It.Is<string>(x => x == configurationItem.Key)])
                    .Returns(configurationItem.Value);

                configurationMock.Setup(x => x.GetSection(configurationItem.Key))
                    .Returns(
                        GetConfigurationSection(configurationItem.Key, configurationItem.Value).Object);
            }

            return configurationMock;
        }

        /// <summary>
        /// Creates a full composed Mock of <see cref="IHttpClientFactory"/>.
        /// </summary>
        /// <returns>A Mock of <see cref="IHttpClientFactory"/>.</returns>
        private static Mock<IHttpClientFactory> GetHttpClientFactoryMock(Mock<HttpClient> mockHttpClient)
        {
            var mockIHttpClientFactory = new Mock<IHttpClientFactory>();      

            mockIHttpClientFactory.Setup(x => 
                x.CreateClient(It.Is<string>(x => 
                    x == MockConfigurationHttpClientName
            )))
                .Returns(mockHttpClient.Object);

            return mockIHttpClientFactory;
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

        /// <summary>
        /// Creates a Handler for the Sample interface and Implementation, creating the dependent mock services if not provided.
        /// Should the mock configuration items be provided, these are leveraged instead.
        /// Also returns all mocked objects to allow for custom assertions per test.
        /// </summary>
        /// <param name="mockILogger">A mock of <see cref="ILogger"/>.</param>
        /// <param name="mockIConfiguration">A mock of <see cref="IConfiguration"/>.</param>
        /// <param name="mockIHttpClientFactory">A mock of <see cref="IHttpClientFactory"/>.</param>
        /// <returns>A <see cref="Tuple"/> composing of the <see cref="RateLimitTester"/> along with a the mocked objects to assert if required.</returns>
        private static (
            RateLimitTester handler, 
            Mock<ILogger<RateLimitTester>> mockILogger,
            Mock<IConfiguration> mockIConfiguration, 
            Mock<IHttpClientFactory> mockIHttpClientFactory, 
            Mock<HttpClient> mockHttpClient) 
            GetHandler(
                Mock<ILogger<RateLimitTester>>? mockILogger = null,
                Mock<IConfiguration>? mockIConfiguration = null,
                Mock<IHttpClientFactory>? mockIHttpClientFactory = null,
                Mock<HttpClient>? mockHttpClient = null
            )
        {
            mockILogger ??= GetMockILogger<RateLimitTester>();
            mockIConfiguration ??= GetConfigurationMock(new Dictionary<string, string>());
            mockHttpClient ??= GetMockHttpClient();
            mockIHttpClientFactory??= GetHttpClientFactoryMock(mockHttpClient);

            return (
                new RateLimitTester(
                    mockILogger.Object, 
                    mockIHttpClientFactory.Object, 
                    mockIConfiguration.Object),
                mockILogger,
                mockIConfiguration,
                mockIHttpClientFactory, 
                mockHttpClient);
        }
    }
}
