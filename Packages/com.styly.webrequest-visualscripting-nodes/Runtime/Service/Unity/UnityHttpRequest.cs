﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine.Networking;

namespace STYLY.Http.Service.Unity
{
    public class UnityHttpRequest : IHttpRequest, IUpdateProgress
    {
        internal UnityWebRequest UnityWebRequest => unityWebRequest;

        private readonly UnityWebRequest unityWebRequest;
        private readonly Dictionary<string, string> headers;

        private event Action<float> onUploadProgress;
        private event Action<float> onDownloadProgress;
        private event Action<HttpResponse> onSuccess;
        private event Action<HttpResponse> onError;
        private event Action<HttpResponse> onNetworkError;

        private float downloadProgress;
        private float uploadProgress;

        public string[] ignorePatternsForCacheFilePathGeneration;
        public string URL;

        public UnityHttpRequest(UnityWebRequest unityWebRequest)
        {
            this.URL = unityWebRequest.url;
            this.unityWebRequest = unityWebRequest;
            headers = new Dictionary<string, string>(Http.GetSuperHeaders());
        }

        public IHttpRequest RemoveSuperHeaders()
        {
            foreach (var kvp in Http.GetSuperHeaders())
            {
                headers.Remove(kvp.Key);
            }

            return this;
        }

        public IHttpRequest SetHeader(string key, string value)
        {
            headers[key] = value;
            return this;
        }

        public IHttpRequest SetHeaders(IEnumerable<KeyValuePair<string, string>> headers)
        {
            foreach (var kvp in headers)
            {
                SetHeader(kvp.Key, kvp.Value);
            }

            return this;
        }

        public IHttpRequest OnUploadProgress(Action<float> onProgress)
        {
            onUploadProgress += onProgress;
            return this;
        }

        public IHttpRequest OnDownloadProgress(Action<float> onProgress)
        {
            onDownloadProgress += onProgress;
            return this;
        }

        public IHttpRequest OnSuccess(Action<HttpResponse> onSuccess)
        {
            this.onSuccess += onSuccess;
            return this;
        }

        public IHttpRequest OnError(Action<HttpResponse> onError)
        {
            this.onError += onError;
            return this;
        }

        public IHttpRequest OnNetworkError(Action<HttpResponse> onNetworkError)
        {
            this.onNetworkError += onNetworkError;
            return this;
        }

        public bool RemoveHeader(string key)
        {
            return headers.Remove(key);
        }

        public IHttpRequest SetTimeout(int duration)
        {
            unityWebRequest.timeout = duration;
            return this;
        }

        public IHttpRequest Send()
        {
            foreach (var header in headers)
            {
                unityWebRequest.SetRequestHeader(header.Key, header.Value);
            }

            Http.Instance.Send(this, onSuccess, onError, onNetworkError);
            return this;
        }

        public IHttpRequest SetRedirectLimit(int redirectLimit)
        {
            UnityWebRequest.redirectLimit = redirectLimit;
            return this;
        }

        public void UpdateProgress()
        {
            UpdateProgress(ref downloadProgress, unityWebRequest.downloadProgress, onDownloadProgress);
            UpdateProgress(ref uploadProgress, unityWebRequest.uploadProgress, onUploadProgress);
        }

        public void Abort()
        {
            Http.Instance.Abort(this);
        }

        private void UpdateProgress(ref float currentProgress, float progress, Action<float> onProgress)
        {
            if (currentProgress < progress)
            {
                currentProgress = progress;
                onProgress?.Invoke(currentProgress);
            }
        }

        public IHttpRequest UseCache(CacheType cacheType = CacheType.UseCacheAlways, string[] ignorePatternsForCacheFilePathGeneration = null)
        {

#if USE_CLOUD_SIGNED_URL_IN_CACHEUTILS
            // Add Cloud Signed URL ignore patterns if contents are stored in cloud storage with signed URL
            ignorePatternsForCacheFilePathGeneration = (ignorePatternsForCacheFilePathGeneration ?? Enumerable.Empty<string>())
                .Concat(CacheUtils.GetSignedUrlIgnorePatters())
                .ToArray();
#endif

            this.ignorePatternsForCacheFilePathGeneration = ignorePatternsForCacheFilePathGeneration;
            Http.Instance.UseCache(this, cacheType);
            return this;
        }
    }
}
