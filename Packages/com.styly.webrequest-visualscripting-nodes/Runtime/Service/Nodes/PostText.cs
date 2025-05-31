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
    [UnitShortTitle("Post Text")]
    [UnitTitle("Post Text")]
    [UnitCategory("Web Request")]
    [UnitSubtitle("Post text data to a URL")]
    public class PostText : Unit
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
        public ValueInput TextData;

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
            TextData = ValueInput<string>("Text Data", "");
            result = ValueOutput<string>("Response Text", (flow) => resultValue);
            progress = ValueOutput<float>("Progress Value", (flow) => progressValue);
        }

        private IEnumerator Enter(Flow flow)
        {
            string url = flow.GetValue<string>(URL);
            string textData = flow.GetValue<string>(TextData);
            HttpResponse httpResponse = null;

            UniTask.Create(async () => { httpResponse = await PostTextTask(url, textData); }).Forget();

            while (httpResponse == null)
            {
                if (progressValue != progressValuePrev)
                {
                    flow.Invoke(outputTrigger_Progress); // Use flow.Invoke
                    progressValuePrev = progressValue;
                }
                yield return null;  // Wait until next frame
            }

            if (httpResponse.IsSuccessful)
            {
                resultValue = httpResponse.Text;
                yield return outputTrigger;
            }
            else
            {
                Debug.LogAssertion("Error:" + httpResponse.StatusCode + " " + httpResponse.Error);
                resultValue = httpResponse.Error; // Store error message
                yield return outputTrigger_Error;
            }
        }

        private async UniTask<HttpResponse> PostTextTask(string url, string textData)
        {
            HttpResponse httpResponse = await Http.Post(url, textData)
                .OnError(response => Debug.Log(response.StatusCode + " " + response.Error))
                .OnNetworkError(response => Debug.Log("NetworkError: " + response.Error))
                .OnUploadProgress(progress => progressValue = progress) // Use OnUploadProgress for POST
                .OnDownloadProgress(progress => progressValue = progress) // Keep for response download
                .SendAsync();
            return httpResponse;
        }
    }
}
