namespace TokenStoreDemo.Function.Apim
{
    internal class MyApimUriBuilder
    {
        private readonly string _apimBaseUrl;

        public MyApimUriBuilder(string baseUrl)
        {
            _apimBaseUrl = baseUrl;
        }

        public string FetchToken(string providerId) 
        {
            return $"{_apimBaseUrl}/token-store/fetch";
        }

        public string ListMessages() 
        {
            return $"{_apimBaseUrl}/users/me/messages?maxResults=10";
        }

        public string GetMessage(string id) 
        {
            return $"{_apimBaseUrl}/users/me/messages/{id}";
        }
    }
}