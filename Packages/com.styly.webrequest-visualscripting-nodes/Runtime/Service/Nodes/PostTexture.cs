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
    [UnitShortTitle("Post Texture")]
    [UnitTitle("Post Texture")]
    [UnitCategory("Web Request")]
    [UnitSubtitle("Post Texture2D data to a URL")]
    public class PostTexture : Unit
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
        public ValueInput TextureData;

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
            TextureData = ValueInput<Texture2D>("Texture Data", null);
            result = ValueOutput<string>("Response Text", (flow) => resultValue); // Typically, server response is text
            progress = ValueOutput<float>("Progress Value", (flow) => progressValue);
        }

        private IEnumerator Enter(Flow flow)
        {
            string url = flow.GetValue<string>(URL);
            Texture2D textureData = flow.GetValue<Texture2D>(TextureData);
            HttpResponse httpResponse = null;

            if (textureData == null)
            {
                Debug.LogError("PostTexture: Texture2D data is null.");
                resultValue = "Error: Texture2D data is null.";
                yield return outputTrigger_Error;
                yield break;
            }

            UniTask.Create(async () => { httpResponse = await PostTextureTask(url, textureData); }).Forget();

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

        private async UniTask<HttpResponse> PostTextureTask(string url, Texture2D texture)
        {
            // Ensure texture is readable, might be needed if texture is not marked as read/write enabled in import settings
            // However, directly accessing pixels like this might be slow or not always possible depending on texture format/compression.
            // A common approach is to use EncodeToPNG() or EncodeToJPG().
            byte[] textureBytes = texture.EncodeToPNG(); // Or EncodeToJPG()
            if (textureBytes == null)
            {
                // Fallback or error if encoding fails
                Debug.LogError("Failed to encode texture to PNG.");
                // Construct a minimal error HttpResponse
                return new HttpResponse { StatusCode = 0, Error = "Failed to encode texture", IsSuccessful = false };
            }

            // The Http.Post method that takes byte[] also requires a contentType.
            // For PNG images, it's "image/png". For JPG, it's "image/jpeg".
            string contentType = "image/png";

            HttpResponse httpResponse = await Http.Post(url, textureBytes, contentType)
                .OnError(response => Debug.Log(response.StatusCode + " " + response.Error))
                .OnNetworkError(response => Debug.Log("NetworkError: " + response.Error))
                .OnUploadProgress(progress => progressValue = progress)
                .OnDownloadProgress(progress => progressValue = progress) // For response download
                .SendAsync();
            return httpResponse;
        }
    }
}
