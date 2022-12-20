using System;
using System.IO;
using System.Net;
using System.Threading;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using System.Text;
class Program
{
    static void Main(string[] args)
    {
        EventHubProducerClient producerClient = new EventHubProducerClient(
                "api-call-ij38sdhs.servicebus.windows.net",
              "api-event-hub",
                new DefaultAzureCredential());
        for (int i = 0; i < 750; ++i)
        {
            
            var url = "https://fmpcloud.io/api/v3/quote/AAPL,FB,MSFT?apikey=87e46edfac1eb9728f37b355a27b905a";

            var request = WebRequest.Create(url);
            request.Method = "GET";

            using var webResponse = request.GetResponse();
            using var webStream = webResponse.GetResponseStream();

            using var reader = new StreamReader(webStream);
            var data = reader.ReadToEnd();

            
            using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

            if (!eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(data))))
            {
                // if it is too large
                throw new Exception($"Event {data} is too large for the batch and cannot be sent.");
            }
            

            try
            {
                // Use the producer client to send the batch of events to the event hub
                await producerClient.SendAsync(eventBatch);
            }
            finally
            {
                await producerClient.DisposeAsync();
            }
            Thread.Sleep(400);
        }
    }
}