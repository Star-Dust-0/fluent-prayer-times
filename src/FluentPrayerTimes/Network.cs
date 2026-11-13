using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FluentPrayerTimes;

internal static class Network
{
    private sealed class RetryHandler : DelegatingHandler
    {
        private static readonly ConcurrentDictionary<string, DateTimeOffset> RetryAt = new();
        internal RetryHandler() : base(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(10) }) { }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel)
        {
            string host = request.RequestUri!.Host;
            if (RetryAt.TryGetValue(host, out var next) && next > DateTimeOffset.UtcNow)
                throw new HttpRequestException("Service retry cooldown", null, HttpStatusCode.TooManyRequests);
            var response = await base.SendAsync(request, cancel);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var delay = response.Headers.RetryAfter?.Delta ?? (response.Headers.RetryAfter?.Date - DateTimeOffset.UtcNow) ?? TimeSpan.FromMinutes(1);
                RetryAt[host] = DateTimeOffset.UtcNow + (delay < TimeSpan.FromSeconds(5) ? TimeSpan.FromSeconds(5) : delay);
                Log.Write("network", host + " rate limited; retry deferred");
            }
            return response;
        }
    }
    internal static readonly HttpClient Http = Create(TimeSpan.FromSeconds(20));
    internal static readonly HttpClient Downloads = Create(Timeout.InfiniteTimeSpan);
    private static HttpClient Create(TimeSpan timeout)
    {
        var client = new HttpClient(new RetryHandler()) { Timeout = timeout };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("FluentPrayerTimes/" + (typeof(Network).Assembly.GetName().Version?.ToString(3) ?? "0.0.0"));
        return client;
    }
}
