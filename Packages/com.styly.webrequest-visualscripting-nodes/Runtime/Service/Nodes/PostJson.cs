using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using System.Collections;
using Unity.VisualScripting;
using STYLY.Http;
using STYLY.Http.Service;
using System.Threading.Tasks;
using STYLY.Http.Service.Unity;

namespace WebrequestVisualScriptingNodes
{
    [UnitShortTitle("Post Json")]
    [UnitTitle("Post Json")]
    [UnitCategory("Web Request")]
    [UnitSubtitle("Post JSON string data to a URL with optional Bearer token")] // Updated subtitle
    public class PostJson : Unit
    {
        [DoNotSerialize]
        public ControlInput inputTrigger;

        [DoNotSerialize]
        public ControlOutput outputTrigger;

        [DoNotSerialize]
        public ControlOutput outputTrigger_Error;

        [DoNotSerialize]
        public ControlOutput outputTrigger_Progress;

        [DoNotSerialize]
        public ValueInput URL;

        [DoNotSerialize]
        public ValueInput JsonData;

        [DoNotSerialize]
        public ValueInput BearerToken; // Added BearerToken

        [DoNotSerialize]
        public ValueOutput result;

        [DoNotSerialize]
        public ValueOutput progress;

        private string resultValue;
        private float progressValue;
        private float progressValuePrev;

        protected override void Definition()
        {
            inputTrigger = ControlInputCoroutine("inputTrigger", Enter);
            outputTrigger = ControlOutput("Success");
            outputTrigger_Error = ControlOutput("Error");
            outputTrigger_Progress = ControlOutput("Progress");

            URL = ValueInput<string>("URL", "");
            JsonData = ValueInput<string>("JSON Data", "");
            BearerToken = ValueInput<string>("Bearer Token", string.Empty); // Initialized BearerToken port
            result = ValueOutput<string>("Response Text", (flow) => resultValue);
            progress = ValueOutput<float>("Progress Value", (flow) => progressValue);
        }

        private IEnumerator Enter(Flow flow)
        {
            string url = flow.GetValue<string>(URL);
            string jsonData = flow.GetValue<string>(JsonData);
            string bearerToken = flow.GetValue<string>(BearerToken); // Get BearerToken value
            HttpResponse httpResponse = null;

            UniTask.Create(async () => { httpResponse = await PostJsonTask(url, jsonData, bearerToken); }).Forget(); // Pass bearerToken

            while (httpResponse == null)
            {
                if (progressValue != progressValuePrev)
                {
                    flow.Invoke(outputTrigger_Progress);
                    progressValuePrev = progressValue;
                }
                yield return null;
            }

            if (httpResponse.IsSuccessful)
            {
                resultValue = httpResponse.Text;
                yield return outputTrigger;
            }
            else
            {
                Debug.LogAssertion("Error:" + httpResponse.StatusCode + " " + httpResponse.Error);
                resultValue = httpResponse.Error;
                yield return outputTrigger_Error;
            }
        }

        private async UniTask<HttpResponse> PostJsonTask(string url, string jsonData, string bearerToken) // Added bearerToken parameter
        {
            var request = Http.PostJson(url, jsonData);
            if (!string.IsNullOrEmpty(bearerToken))
            {
                request.SetHeader("Authorization", "Bearer " + bearerToken); // Set Authorization header
            }

            HttpResponse httpResponse = await request
                .OnError(response => Debug.Log(response.StatusCode + " " + response.Error))
                .OnNetworkError(response => Debug.Log("NetworkError: " + response.Error))
                .OnUploadProgress(progress => progressValue = progress)
                .OnDownloadProgress(progress => progressValue = progress)
                .SendAsync();
            return httpResponse;
        }
    }
}
