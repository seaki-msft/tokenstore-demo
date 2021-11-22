using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using TokenStoreDemo.Function.Apim;

namespace TokenStoreDemo.Function
{
    public static class GmailToDropbox
    {
        [FunctionName("GmailToDropbox")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string name = req.Query["name"];

            var apimService = new ApimService("https://seaki-westcentralus-test.azure-api.net/");
            await RunUsingGetTokenBackAsync(apimService);
            await RunUsingAttachTokenToBackendAsync(apimService);


            return new OkObjectResult("hello");
        }

        private static async Task RunUsingGetTokenBackAsync(ApimService apimService)
        {
            var token = await apimService.GetTokenBackAsync("google1", "auth1");
            var gmailService = GoogleCustomTokenHttpClientInitializer.InitializeGmailService(token);
            var messages = await gmailService.Users.Messages.List("me").ExecuteAsync();

            foreach (var message in messages.Messages) {
                var fullMessage = await gmailService.Users.Messages.Get("me", message.Id).ExecuteAsync();
                var attachmentIds = message.Payload.Parts.Select(p => p.Body.AttachmentId).Where(aid => aid != null);

                foreach (var attachmentId in attachmentIds) {
                    var attachment = await gmailService.Users.Messages.Attachments.Get("me", message.Id, attachmentId).ExecuteAsync();
                    // TODO(seaki): upload to dropbox
                }
            }

            return;
        }

        
        private static async Task RunUsingAttachTokenToBackendAsync(ApimService apimService)
        {
            // await apimService.Get
        }
    }
}
