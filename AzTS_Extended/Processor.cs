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
            string workItem = "{\"SubscriptionId\":\"<Insert_Subscription_ID_Here>\",\"Timestamp\":\"Sun, 01 Mar 2020 14:39:49 GMT\",\"JobId\":20200229,\"RetryCount\":0, \"IsRBACProcessed\": true}";
            var workItemObject = JsonConvert.DeserializeObject<SubscriptionWorkItem>(workItem);
            workItemObject.IsRBACProcessed = false;
            _subscriptionItemProcessor._log = log;
            _subscriptionItemProcessor.ProcessSubscriptionItems(workItemObject);
        }
    }
}