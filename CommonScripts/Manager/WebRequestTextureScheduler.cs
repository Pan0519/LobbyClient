using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Threading.Tasks;
using UnityEngine;
using System;

public class WebRequestTextureScheduler : MonoSingleton<WebRequestTextureScheduler>
{

    public LoadingInfo request(string url, Action<Texture2D> callback)
    {
        LoadingInfo requestInfo = new LoadingInfo(url, callback);
        return requestInfo;
    }

    public class LoadingInfo
    {
        public bool isCanceled
        {
            get { return canceld; }
            private set { canceld = value; }
        }

        public bool isDownloading
        {
            get { return downloading; }
            private set { downloading = value; }
        }

        bool canceld;
        bool downloading;
        string url;
        Texture2D downloadResult;
        Action<Texture2D> onDone = null;

        int connectTime = 1;
        int maxConnectTime = 10;

        public LoadingInfo(string url, Action<Texture2D> callback)
        {
            this.url = url;
            onDone = callback;
        }
        public void cancel()
        {
            isCanceled = true;
        }

        public async void download()
        {
            isDownloading = true;
            downloadResult = null;

            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
            {
                await www.SendWebRequest();
                switch (www.result)
                {
                    case UnityWebRequest.Result.Success:
                        downloadResult = DownloadHandlerTexture.GetContent(www);
                        isDownloading = false;
                        break;

                    case UnityWebRequest.Result.ConnectionError:
                        await Task.Delay(TimeSpan.FromSeconds(1f));
                        download();
                        return;

                    case UnityWebRequest.Result.ProtocolError:
                        ++connectTime;
                        if (connectTime < maxConnectTime)
                        {
                            download();
                            return;
                        }
                        break;
                }
            }

            onDone.trigger(downloadResult);
            isDownloading = false;
        }

        //public void fireFinishEvent()
        //{
        //    onDone.trigger(downloadResult);
        //}
    }
}
