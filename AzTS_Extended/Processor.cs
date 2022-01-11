namespace AzTS_Extended
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Microsoft.AzSK.ATS.Extensions;
    using Microsoft.AzSK.ATS.Extensions.Models;
    using Microsoft.AzSK.ATS.ProcessSubscriptions.Models;
    using Microsoft.AzSK.ATS.ProcessSubscriptions.Processors;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    public class Processor
    {
        private SubscriptionItemProcessor _subscriptionItemProcessor;

        public Processor(
            SubscriptionItemProcessor subscriptionItemProcessor)
        {
            _subscriptionItemProcessor = subscriptionItemProcessor;
        }

        // This function will get triggered/executed when a new message is written
        // on an Azure Queue called queue.
        [FunctionName("SubWorkItemProcessor")]
        //public void Run([QueueTrigger("%queueTriggerName%")] string workItem, ILogger log)

        public void Run([TimerTrigger("0 */60 0-12 * * *", RunOnStartup = true)] TimerInfo timer, ILogger log)
        {
            // **Remember**: Before deploying, comment the `line 31` and `line 34` and comment out the `line 29` to make the *Run* function as queue-triggered instead of timer-triggered which is done for local testing purposes. 
            string workItem = "{\"SubscriptionId\":\"<subscriptionId>\"}";
            var workItemObject = JsonConvert.DeserializeObject<SubscriptionWorkItem>(workItem);
            workItemObject.IsRBACProcessed = false;
            _subscriptionItemProcessor._log = log;
           var controlResults = _subscriptionItemProcessor.ProcessSubscriptionItems(workItemObject);

        }
    }
}
