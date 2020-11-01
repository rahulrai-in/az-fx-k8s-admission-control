using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using az_fx_k8s_admission_control.Contracts;
using Azure.Communication;
using Azure.Communication.Sms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace RR.AKSAdmissionController
{
    public static class AdmissionControlFx
    {
        private const string NewAppMessage =
            "Beep Boop! I am the admission bot.\n\nI have approved the deployment of the application {0} on {1}. The current deployment comprises these container images: {2}.\n\n{0} is a new application on the cluster. Welcome!";

        private const string UpgradeAppMessage =
            "Beep Boop! I am the admission bot.\n\nI have approved the update of the application {0} on {1}. The current deployment comprises these container images {2}. Previously, {0} had these container images: {3}.\n\nPlease verify the deployment.";

        private const string FailedAppMessage =
            "Beep Boop! I am the admission bot.\n\nI have blocked the deployment of the application {0} on {1}. A minimum of 3 replicas is necessary for high availability.\n\nPlease increase the replica count and redeploy {0}.";

        [FunctionName("AdmissionControlFx")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
            HttpRequest req)
        {
            string sms;
            var requestBodyString = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic requestBody = JsonConvert.DeserializeObject<ExpandoObject>(requestBodyString);
            string operation = requestBody.request.operation; // CREATE or UPDATE

            // Read name of application from the label `app`.
            string app = requestBody?.request?.@object?.metadata?.labels?.app;

            // Replica count check. You can make it as complex as you like.
            var replicaCount = requestBody?.request?.@object?.spec?.replicas ?? 0;
            if (replicaCount < 3)
            {
                sms = string.Format(FailedAppMessage, app.ToUpperInvariant(), DateTime.UtcNow.ToString("r"));
                SendSms();
                return new OkObjectResult(new ResponseModel(requestBody.request.uid, false,
                    "replica count should be greater than 2"));
            }

            // Extract container image.
            IEnumerable<dynamic> candidateContainers =
                requestBody?.request?.@object?.spec?.template?.spec?.containers;
            var candidateImages = string.Join(',', candidateContainers.Select(cc => (string) cc.image));

            // For create, dispatch the message immediately.
            if (operation.Equals("create", StringComparison.OrdinalIgnoreCase))
            {
                sms = string.Format(NewAppMessage, app.ToUpperInvariant(), DateTime.UtcNow.ToString("r"),
                    candidateImages);
            }
            else
            {
                // Extract image details from the old deployment.
                IEnumerable<dynamic> existingContainers =
                    requestBody?.request?.oldObject?.spec?.template?.spec?.containers;
                var existingImages = string.Join(',', existingContainers.Select(cc => (string) cc.image));
                sms = string.Format(UpgradeAppMessage, app.ToUpperInvariant(), DateTime.UtcNow.ToString("r"),
                    candidateImages,
                    existingImages);
            }

            SendSms();

            // Return result to Kubernetes
            return new OkObjectResult(new ResponseModel(requestBody.request.uid, true));

            void SendSms()
            {
                // Send SMS using Azure ACS: https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/telephony-sms/send
                var teamPhone =
                    Environment.GetEnvironmentVariable("SupportTeamPhone", EnvironmentVariableTarget.Process);
                var connectionString =
                    Environment.GetEnvironmentVariable("ACS_ConnectionString", EnvironmentVariableTarget.Process);
                var fromPhoneNumber =
                    Environment.GetEnvironmentVariable("ACS_FromPhoneNumber", EnvironmentVariableTarget.Process);
                var smsClient = new SmsClient(connectionString!);
                var response = smsClient.Send(
                    new PhoneNumber(fromPhoneNumber!),
                    new PhoneNumber(teamPhone!),
                    sms);
            }
        }
    }
}