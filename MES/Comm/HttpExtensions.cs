using Polly;
using Polly.Extensions.Http;
using Polly.Wrap;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MES.Comm
{
    public static class HttpExtensions
    {
        public static async Task<T> ReadJson<T>(this HttpResponseMessage httpResponseMessage)
        {
            string message = await httpResponseMessage.Content.ReadAsStringAsync();

            T? target = JSON.FromJson<T>(message);

            return target;
        }

        public static async Task<(bool result, T target)> TryReadJson<T>(this HttpResponseMessage httpResponseMessage)
        {
            string message = await httpResponseMessage.Content.ReadAsStringAsync();

            if (JSON.TryParse<T>(message, out T? target))
            {
                return (true, target!);
            }

            return (false, default!);
        }

        /// <summary>
        /// 大概20S左右超时返回
        /// <para></para>
        /// <para></para>
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="uri"></param>
        /// <param name="httpContent"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> PollyPostJsonAsync(
            this HttpClient httpClient,
            string uri,
            string json)
        {
            var policy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TaskCanceledException>()
                .Or<WebException>()
                .Or<SocketException>()
                .WaitAndRetryAsync(
                    retryCount: 1,
                    sleepDurationProvider: _ => TimeSpan.FromSeconds(3)
                );

            try
            {
                return await policy.ExecuteAsync(async () =>
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await httpClient.PostAsync(uri, content);
                });
            }
            catch (Exception ex) when (
                ex is HttpRequestException ||
                ex is TaskCanceledException ||
                ex is WebException ||
                ex is SocketException)
            {
                throw new Exception($"MES网络请求失败，3秒后已重试1次仍失败：{ex.Message}");
            }
        }




        #region Polly

        private static readonly Func<IAsyncPolicy<HttpResponseMessage>> policyAsyncCreator =
            new(
                () =>
                    HttpPolicyExtensions
                        .HandleTransientHttpError()
                        .OrTransientHttpError()
                        .OrTransientHttpStatusCode()
                        .WaitAndRetryAsync(2, i => TimeSpan.FromSeconds(Math.Pow(2, i)))
                        .WrapAsync(Policy.TimeoutAsync((int)Math.Pow(2, 6)))
            );

        private static readonly Func<PolicyWrap<HttpResponseMessage>> policyCreator =
            new(
                () =>
                    HttpPolicyExtensions
                        .HandleTransientHttpError()
                        .OrTransientHttpError()
                        .OrTransientHttpStatusCode()
                        .WaitAndRetry(2, i => TimeSpan.FromSeconds(Math.Pow(2, i)))
                        .Wrap(Policy.Timeout((int)Math.Pow(2, 6)))
            );

        #endregion
    }
}
