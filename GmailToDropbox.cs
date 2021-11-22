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
using Microsoft.AspNetCore.WebUtilities;

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
            // Get Attachment from Gmail
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
            var attachment = await gmailService.Users.Messages.Attachments.Get(
                "me", "17d3a9446fd7c94a", "ANGjdJ8-nGL_qcyi0LW1tBjZx6FPZwBa7GENOniTHnbRNIKc-fpM57CNASykfHZfwkM2zxDT4WWkACFp3iEJ9q7LEzkpcSUMZ8xVcwhjDwDzzcuFHaae5Pl1pvDAP3LyQ7azE5m-77JpsU8Hc6XlFW4_qbXejeCsebsXaWC_0gFxJWtvK9BQSUF1kEGzsR48lKWrA2TKkFwxdaAcW038dxviAANZ1B6anrw9yxt8BwAwTc8FVtpx59Q0asaJk7lv96aPi0T8pG4eErj0WhyIwe2RfvdfbHvGJi-SjuNNoaqnKrMx8caVOu_P8Z3AVqA"
                ).ExecuteAsync();
            var attachmentRawContent = (Base64UrlTextEncoder.Decode(attachment.Data)).ToString();

            // Dropbox
            var result = await apimService.DropboxUploadFileAsync("temp.txt", attachmentRawContent);
            return result;
        }
    }
}
