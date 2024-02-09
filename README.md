# SimpleRateLimiter

![build](https://github.com/andyjmorgan/DonkeyWork.Dev.SimpleRateLimiter/actions/workflows/dotnet-build.yml/badge.svg) ![codeql](https://github.com/andyjmorgan/DonkeyWork.Dev.SimpleRateLimiter/actions/workflows/codeql.yml/badge.svg)

A simple httpclient middleware for rate limiting. This library supports dependency injection and chaining with such libraries as Polly.

To use in a console application: (with a per second limit of 10):
```
new HttpClient(new SimpleRateLimitHandler(requestsPerSecond: 10))
{
    BaseAddress = new Uri(Properties.Resources.BaseAddress)
};
```

To use with Dependency Injection:
```
builderContext.Configuration.GetValue<string>("HttpClientName", "DonkeyWork")!)
  .ConfigureHttpClient(options =>
  {
      options.BaseAddress = new Uri(builderContext.Configuration.GetValue<string>("BaseAddress")!);
  })
  .AddHttpMessageHandler(() =>
      new SimpleRateLimitHandler(
          requestsPerSecond: 2));
```
