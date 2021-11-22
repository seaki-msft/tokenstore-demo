using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using TokenStoreDemo.Function.Apim;
using Microsoft.Azure.Services.AppAuthentication;
using Google.Apis.Gmail.v1;

namespace TokenStoreDemo.Function
{
    public static class GmailToDropbox
    {
        private const string TokenStoreAudience ="https://management.core.windows.net/";
        private const string TokenStoreAudience1 ="https://apihub.azure.com";

        [FunctionName("GmailToDropbox")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string name = req.Query["name"];

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var msiToken = await azureServiceTokenProvider.GetAccessTokenAsync(TokenStoreAudience);

            var apimService = new ApimService("https://seaki-westcentralus-test.azure-api.net/", msiToken);
            var result = await RunUsingGetTokenBackEndpointAsync(apimService);

            return new OkObjectResult(result);
        }

        private static async Task<object> RunUsingGetTokenBackEndpointAsync(ApimService apimService)
        {
            var gmailToken = await apimService.GetTokenBackBackAsync("google1", "auth1");
            var gmailService = new GmailService(
                new Google.Apis.Services.BaseClientService.Initializer() {
                    HttpClientInitializer = new GoogleCustomTokenHttpClientInitializer(gmailToken)
                });
            
            var messages = await gmailService.Users.Messages.List("me").ExecuteAsync();
            // foreach (var message in messages.Messages) {
            //     var fullMessage = await gmailService.Users.Messages.Get("me", message.Id).ExecuteAsync();
            //     var attachmentIds = message.Payload.Parts?.Select(p => p.Body.AttachmentId?Where(aid => aid != null);

            //     foreach (var attachmentId in attachmentIds) {
            //         var attachment = await gmailService.Users.Messages.Attachments.Get("me", message.Id, attachmentId).ExecuteAsync();
            //         // TODO(seaki): upload to dropbox
            //     }
            // }

            return messages;
        }

        // TODO(seaki): finish
        private static async Task RunUsingAttachTokenToBackendEndpointAsync(ApimService apimService)
        {
            // await apimService.Get
        }
    }
}
