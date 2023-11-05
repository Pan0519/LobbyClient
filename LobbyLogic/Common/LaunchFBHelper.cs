using System;
using UnityEngine;

namespace LobbyLogic.Common
{
    public class LaunchFBHelper
    {
        private AndroidJavaClass unityPlayer = null;
        private AndroidJavaObject activityObj = null;
        private AndroidJavaObject packageManager = null;
        private AndroidJavaObject launchIntent = null;

        private const string fbAppBundleId = "com.facebook.katana";
        private const string messengerBundleId = "com.facebook.orca";
        private const string unityName = "com.unity3d.player.UnityPlayer";
        private const string activityName = "currentActivity";
        private const string javaGetPackageManager = "getPackageManager";
        private const string javaLaunchIntent = "getLaunchIntentForPackage";
        private const string messengerURL = "https://m.me/yuegefb";
        private const string fbAppURL = "fb://profile/100085832168229";
        private const string fbWebURL = "https://www.facebook.com/yuegefb";


        public string getURL()
        {
            var result = string.Empty;

            connectJavaObj();
            result = getConnectURL();
            dispose();

            return result;
        }

        private void connectJavaObj()
        {
            unityPlayer = new AndroidJavaClass(unityName);
            activityObj = unityPlayer.GetStatic<AndroidJavaObject>(activityName);
            try
            {
                packageManager = activityObj.Call<AndroidJavaObject>(javaGetPackageManager);
            }
            catch (Exception e)
            {
                Debug.LogError("Connect Java Error : " + e.ToString());
            }
        }

        private string getConnectURL()
        {
            if (checkHaveBundle(messengerBundleId))
            {
                return messengerURL;
            }

            if (checkHaveBundle(fbAppBundleId))
            {
                return fbAppURL;
            }

            return fbWebURL;
        }

        private bool checkHaveBundle(string bundleId)
        {
            try
            {
                launchIntent = packageManager.Call<AndroidJavaObject>(javaLaunchIntent, bundleId);
                return (null != launchIntent);
            }
            catch (Exception e)
            {
                Debug.LogError("Obtain APP Bundle Error : " + e.ToString());
                return false;
            }
        }

        private void dispose()
        {
            if (null != launchIntent)
            {
                launchIntent.Dispose();
            }
            packageManager.Dispose();
            activityObj.Dispose();
            unityPlayer.Dispose();

            launchIntent = null;
            packageManager = null;
            activityObj = null;
            unityPlayer = null;
        }
    }
}