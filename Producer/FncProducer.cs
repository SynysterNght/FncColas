using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace Producer
{



    public static class FncProducer
    {
        [FunctionName("FProducer")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            
            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;
            //await nameof(PostMessagesToServiceBusQueue);

            var messages = Enumerable.Range(1, 1)
                   .Select(m =>
                   {
                       return new SessionMessagesCreateRequest
                       {
                           SessionId = "1",
                           MessageId = 1,
                       };
                   }).ToList();
            

            await PostMessagesToServiceBusQueue(log);
            
            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }


        private static readonly Lazy<byte[]> _messageContent = new Lazy<byte[]>(() =>
        {
                string test = "ESte prueba computacion ubicua";

                // convert string to stream
                byte[] byteArray = Encoding.ASCII.GetBytes(test);
                MemoryStream stream = new MemoryStream(byteArray);

                // convert stream to string
                StreamReader sr = new StreamReader(stream);
                //(StreamReader sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream($@"messagecontent.txt")));

                return Encoding.Default.GetBytes(sr.ReadToEnd());
        });

        private const int MAX_RETRY_ATTEMPTS = 10;

        [FunctionName(nameof(PostMessagesToServiceBusQueue))]
        public static async Task PostMessagesToServiceBusQueue(
                [ServiceBus("%ServiceBusQueueName%", Connection = @"ServiceBusConnection")]IAsyncCollector<Message> queueMessages,
                ILogger log)
        {

            var messageToPost = new Message {
                Body = _messageContent.Value,
                ContentType = @"text/plain",    // feel free to change this if your content is JSON (application/json), XML (application/xml), etc
                CorrelationId = "1",
                MessageId = $@"1/1",    // this property is used for de-duping
                ScheduledEnqueueTimeUtc = DateTime.UtcNow,
                SessionId = "1"
            };
            var retryCount = 0;
            var retry = false;
            do
            {
                retryCount++;
                try
                {
                    await queueMessages.AddAsync(messageToPost);
                    retry = false;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, $@"Error posting message for session '{messageToPost.SessionId}'. Retrying...");
                    retry = true;
                }

                if (retry && retryCount >= MAX_RETRY_ATTEMPTS)
                {
                    log.LogError($@"Unable to post message to {messageToPost.SessionId} after {retryCount} attempt(s). Giving up.");
                    break;
                }
                else
                {
                    #if DEBUG
                        log.LogTrace($@"Posted message {messageToPost.MessageId} (Size {messageToPost.Body.Length} bytes) for session '{messageToPost.SessionId}' in {retryCount} attempt(s)");
                    #else
                        log.LogTrace($@"Posted message for session '{messageToPost.SessionId}' in {retryCount} attempt(s)");
                    #endif
                }
            } while (retry);


        }

    }
}


