using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.Azure.Services.AppAuthentication;
using Google.Apis.Gmail.v1;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Collections.Generic;
using System;
using Microsoft.Net.Http.Headers;

namespace TokenStoreDemo.Function
{
    public class GmailToDropbox
    {
        private readonly string[] AllowedTokenStoreAudience = new string[] { "https://management.core.windows.net/", "https://apihub.azure.com" };

        [FunctionName("GmailToDropbox")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var msiToken = await azureServiceTokenProvider.GetAccessTokenAsync(AllowedTokenStoreAudience.First());

            // Set environment variables in local.settings.json (local development) and Configuration (Azure)
            var myApimEndpoint = Environment.GetEnvironmentVariable("MyApimEndpoint") ?? throw new Exception("MyApimEndpoint Environment variable not set");
            var mySubscriptionKey = Environment.GetEnvironmentVariable("MyApimSubscriptionKey") ?? throw new Exception("MyApimSubscriptionKey Environment variable not set");
            var apimService = new ApimService(myApimEndpoint, mySubscriptionKey, msiToken);
            var uploadedFileNames = await UploadLatestAttachmentToGmail(apimService);

            var message = $"Uploaded the following files to dropbox: {string.Join(',', uploadedFileNames)}";
            log.LogInformation(message);
            return new OkObjectResult(message);
        }

        private async Task<List<string>> UploadLatestAttachmentToGmail(ApimService apimService)
        {
            var uploadedFileNames = new List<string>();

            // Get Attachment from Gmail
            var gmailToken = await apimService.GetTokenBackBackAsync("google1", "auth1");
            var gmailService = new GmailService(
                new Google.Apis.Services.BaseClientService.Initializer() {
                    HttpClientInitializer = new GoogleCustomTokenHttpClientInitializer(gmailToken)
                });
            
            var messages = await gmailService.Users.Messages.List("me").ExecuteAsync();
            foreach (var message in messages.Messages) {
                var fullMessage = await gmailService.Users.Messages.Get("me", message.Id).ExecuteAsync();
                var attachmentParts = fullMessage.Payload?.Parts?.Where(p => p?.Body?.AttachmentId != null);
                if (attachmentParts == null) {
                    continue;
                }

                foreach (var attachmentPart in attachmentParts) {
                    var attachment = await gmailService.Users.Messages.Attachments.Get("me", message.Id, attachmentPart.Body.AttachmentId).ExecuteAsync();
                    var attachmentRawContent = Encoding.Default.GetString(Base64UrlTextEncoder.Decode(attachment.Data));

                    // Upload to Dropbox
                    var result = await apimService.DropboxUploadFileAsync(attachmentPart.Filename, attachmentRawContent);
                    uploadedFileNames.Add(result["name"].ToString());
                }

                // For simplicity, upload just the latest attachment
                break; 
            }
            return uploadedFileNames;
        }
    }
}
