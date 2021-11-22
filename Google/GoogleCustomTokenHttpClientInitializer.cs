using Google.Apis.Gmail.v1;
using Google.Apis.Http;

namespace TokenStoreDemo.Function
{
    public class GoogleCustomTokenHttpClientInitializer : IConfigurableHttpClientInitializer
    {
        private readonly string _bearerToken;

        public GoogleCustomTokenHttpClientInitializer(string bearerToken) {
            _bearerToken = bearerToken;
        }
        
        public void Initialize(ConfigurableHttpClient httpClient) {
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_bearerToken}");
        }

        public static GmailService InitializeGmailService(string bearerToken) {
            var service = new GmailService(
                new Google.Apis.Services.BaseClientService.Initializer() {
                    HttpClientInitializer = new GoogleCustomTokenHttpClientInitializer(bearerToken)
                });
            return service;
        }
    }
}