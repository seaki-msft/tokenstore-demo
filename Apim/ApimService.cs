using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Google.Apis.Gmail.v1.Data;

namespace TokenStoreDemo.Function.Apim
{
    internal class ApimService
    {
        private readonly string _apimBaseUrl;
        private readonly HttpClient _httpClient;

        public ApimService(string baseUrl)
        {
            _apimBaseUrl = baseUrl;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "0c0b8b18728c4c868dacaaf1d8d9c133");
        }

        public async Task<string> GetTokenBackAsync(string providerId, string authorizationId) {
            var endpoint = $"{_apimBaseUrl}/token-store/fetch?provider-id={providerId}&authorization-id={authorizationId}";
            var response = await _httpClient.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) {
                throw new Exception($"Fetch token call unsuccessful: {content}");
            }
            return content;
        }

        public async Task<string> GetMessageIdAsync(int maxResult = 10) {
            var response = await _httpClient.GetAsync($"{_apimBaseUrl}/users/me/messages?maxResults={maxResult}");
            var messages = await response.Content.ReadAsAsync<ListMessagesResponse>();            
            var latestMessageId = messages.Messages.LastOrDefault()?.Id;
            return latestMessageId;
        }

        public async Task<string> GetAnyAttachmentIdAsync(string messageId) {
            var response = await _httpClient.GetAsync($"{_apimBaseUrl}/users/me/messages/{messageId}");
            var message = await response.Content.ReadAsAsync<Message>();
            var aid = message.Payload.Parts.Select(p => p.Body.AttachmentId).FirstOrDefault(aid => aid != null);
            return aid;
        }
    }
}