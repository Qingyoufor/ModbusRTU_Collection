using Client.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Client.Infrastructure
{
    /*
     HttpClient 负责：联系前后端
       - 建立 TCP 连接
       - 发送 HTTP 请求报文
       - 接收 HTTP 响应报文
       - 解析成 HttpResponseMessage
     */
    public class HttpClientBase
    {
        private readonly HttpClient _httpClient;
        private readonly TokenStore _tokenStore;
        private readonly SemaphoreSlim _refreshTokenLock = new(1, 1); //异步锁，只允许一个线程进入信号量锁
        private static readonly JsonSerializerOptions _jsonOptions = new () { PropertyNameCaseInsensitive = true };

        private const string BaseUrl = "http://localhost:5078/";

        public HttpClientBase(TokenStore tokenStore)
        {
            // 和相对路径进行拼接，如"/api/auth/refresh"
            _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            _tokenStore = tokenStore;
        }

        //===========公开的 HTTP CRUD 方法==========
        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            return await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, url)); 
        }

        public async Task<HttpResponseMessage> PostAsync<T>(string url,T body)
        {
            var requset = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(body, options: _jsonOptions)
            };
            return await SendAsync(() => requset);
        }

        public async Task<HttpResponseMessage> PutAsync<T>(string url, T body)
        {
            var requset = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = JsonContent.Create(body, options: _jsonOptions)
            };
            return await SendAsync(() => requset);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string url)
        {
            return await SendAsync(() => new HttpRequestMessage(HttpMethod.Delete, url));
        }

        // ======核心发送逻辑=======
        private async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> requestFactory) 
        {
            // 1.首次发送
            var request = requestFactory();
            AttachToken(request);
            var respone = await _httpClient.SendAsync(request);

            // 2.如果401，尝试刷新Token
            if(respone.StatusCode == HttpStatusCode.Unauthorized)
            {
                var isRefresh = await TryRefreshTokenAsync();
                // 如果刷新成功，则重放原请求
                if (isRefresh)
                {
                    request = requestFactory();
                    AttachToken(request);
                    respone = await _httpClient.SendAsync(request);
                }
            }

            return respone;
        }

        // 给请求附加token
        private void AttachToken(HttpRequestMessage request) 
        {
            var accessToken = _tokenStore.AccessToken;
            if(!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        // 尝试刷新token
        private async Task<bool> TryRefreshTokenAsync() 
        {
            await _refreshTokenLock.WaitAsync(); // 获取钥匙(锁)
            try
            {
                // 请求刷新token（可以在此方法前添加token过期识别，如果过期了再进行下面的刷新操作）
                var response = await _httpClient.PostAsJsonAsync("api/auth/refresh", new TokenStore
                {
                    AccessToken = _tokenStore.AccessToken,
                    RefreshToken = _tokenStore.RefreshToken
                }, _jsonOptions); 
                
                if(!response.IsSuccessStatusCode) return false;

                // json反序列化成目标泛行
                var result = await response.Content.ReadFromJsonAsync<ApiResult<LoginResponse>>(_jsonOptions);
                if(result == null || result.Data == null || result.Code != 200) return false;

                _tokenStore.SetTokens(result.Data.AccseeToken, result.Data.RefreshToken);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                _refreshTokenLock.Release(); // 释放钥匙
            }
        }

       
    }
}
