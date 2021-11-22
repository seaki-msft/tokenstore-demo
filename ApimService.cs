using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Google.Apis.Gmail.v1.Data;
using Newtonsoft.Json.Linq;

namespace TokenStoreDemo.Function
{
    internal class ApimService
    {
        private readonly string _apimBaseUrl;
        private readonly HttpClient _httpClient;

        public ApimService(string baseUrl, string subscriptionKey, string identityToken)
        {
            _apimBaseUrl = baseUrl;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", identityToken);
        }

        public async Task<string> GetTokenBackAsync(string providerId, string authorizationId)
        {
            var endpoint = $"{_apimBaseUrl}/token-store/fetch?provider-id={providerId}&authorization-id={authorizationId}";
            var response = await _httpClient.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Fetch token call unsuccessful: {content}");
            }
            return content;
        }

        public async Task<string> GmailGetMessageIdAsync(int maxResult = 10)
        {
            var response = await _httpClient.GetAsync($"{_apimBaseUrl}/users/me/messages?maxResults={maxResult}");
            var messages = await response.Content.ReadAsAsync<ListMessagesResponse>();
            var latestMessageId = messages.Messages.LastOrDefault()?.Id;
            return latestMessageId;
        }

        public async Task<string> GmailGetAnyAttachmentIdAsync(string messageId)
        {
            var response = await _httpClient.GetAsync($"{_apimBaseUrl}/users/me/messages/{messageId}");
            var message = await response.Content.ReadAsAsync<Message>();
            var aid = message.Payload.Parts.Select(p => p.Body.AttachmentId).FirstOrDefault(aid => aid != null);
            return aid;
        }

        public async Task<JObject> DropboxUploadFileAsync(string filename, string rawData)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_apimBaseUrl}/dropbox/files/upload");
            request.Content = new StringContent(rawData);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            request.Headers.Add("Dropbox-API-Arg", $"{{\"path\": \"/{filename}\",\"mode\": \"add\",\"autorename\": true,\"mute\": false,\"strict_conflict\": false}}");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JObject.Parse(content);
        }
    }
}