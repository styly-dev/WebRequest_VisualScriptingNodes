using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using System.Collections;
using Unity.VisualScripting;
using STYLY.Http;
using STYLY.Http.Service;
using System.Threading.Tasks;
using STYLY.Http.Service.Unity; // Required for Http class if not already there

namespace WebrequestVisualScriptingNodes
{
    [UnitShortTitle("Post Json")]
    [UnitTitle("Post Json")]
    [UnitCategory("Web Request")]
    [UnitSubtitle("Post JSON string data to a URL")]
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
            result = ValueOutput<string>("Response Text", (flow) => resultValue);
            progress = ValueOutput<float>("Progress Value", (flow) => progressValue);
        }

        private IEnumerator Enter(Flow flow)
        {
            string url = flow.GetValue<string>(URL);
            string jsonData = flow.GetValue<string>(JsonData);
            HttpResponse httpResponse = null;

            UniTask.Create(async () => { httpResponse = await PostJsonTask(url, jsonData); }).Forget();

            while (httpResponse == null)
            {
                if (progressValue != progressValuePrev)
                {
                    flow.Invoke(outputTrigger_Progress);
                    progressValuePrev = progressValue;
                }
                yield return null; // Wait until next frame
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

        private async UniTask<HttpResponse> PostJsonTask(string url, string jsonData)
        {
            // Http.PostJson automatically sets the Content-Type to "application/json"
            HttpResponse httpResponse = await Http.PostJson(url, jsonData)
                .OnError(response => Debug.Log(response.StatusCode + " " + response.Error))
                .OnNetworkError(response => Debug.Log("NetworkError: " + response.Error))
                .OnUploadProgress(progress => progressValue = progress)
                .OnDownloadProgress(progress => progressValue = progress) // For response download
                .SendAsync();
            return httpResponse;
        }
    }
}
