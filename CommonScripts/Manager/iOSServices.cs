using UnityEngine;
using System.Runtime.InteropServices;
using System;

using UniRx;
using System.Text;
using AppleAuth.Enums;

#if UNITY_IOS && !UNITY_EDITOR
using AppleAuth;
using AppleAuth.Native;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
#endif

public class iOSServices
{
    static iOSServices instance = null;

    public static iOSServices Instance
    {
        get
        {
            if (null == instance)
            {
                instance = new iOSServices();
                instance.initAppleManager();
            }
            return instance;
        }
    }

#if UNITY_IOS && !UNITY_EDITOR
    IAppleAuthManager appleAuthManager;
#endif

    IObservable<long> managerObservable;
    IDisposable managerDisposable;

    void initAppleManager()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (null != appleAuthManager)
        {
            return;
        }

        var deserializer = new PayloadDeserializer();
        appleAuthManager = new AppleAuthManager(deserializer);

        managerObservable = Observable.Timer(TimeSpan.FromSeconds(0.5f));
        managerDisposable = managerObservable.Subscribe(next, null, complete);
#endif
    }

    void next(long time)
    {
#if UNITY_IOS && !UNITY_EDITOR
        appleAuthManager.Update();
#endif
    }

    void complete()
    {
#if UNITY_IOS && !UNITY_EDITOR
        var replay = managerObservable.Replay();
        var refCount = replay.RefCount();
        refCount.Subscribe(next, complete);
#endif

    }

    public void appleQuickLogin(Action<string> loginCallback)
    {
#if UNITY_IOS && !UNITY_EDITOR
        var quickLogin = new AppleAuthQuickLoginArgs();
        appleAuthManager.QuickLogin(quickLogin,
            credential =>
            {
                var appleIdCredential = credential as IAppleIDCredential;
                string token = Encoding.UTF8.GetString(appleIdCredential.IdentityToken, 0, appleIdCredential.IdentityToken.Length);
                loginCallback?.Invoke(token);
                managerDisposable.Dispose();

            }, error =>
            {
                Debug.LogError($"Apple Qucik Login Failed ErrorCode:{error.GetAuthorizationErrorCode()} , Msg {error}");
            });
#endif
    }

    public void appleLogin(Action<string> loginCallback)
    {
#if UNITY_IOS && !UNITY_EDITOR
        var authLogin = new AppleAuthLoginArgs(LoginOptions.IncludeEmail);
        appleAuthManager.LoginWithAppleId(authLogin,
            credential =>
            {
                var appleIdCredential = credential as IAppleIDCredential;
                string token = Encoding.UTF8.GetString(appleIdCredential.IdentityToken, 0, appleIdCredential.IdentityToken.Length);
                loginCallback?.Invoke(token);
                managerDisposable.Dispose();

            }, error =>
            {
                Debug.LogError($"Apple Login Failed ErrorCode:{error.GetAuthorizationErrorCode()} , Msg {error}");
            });
#endif
    }

    public string getOnlyIDByKeyChain()
    {
#if UNITY_IOS && !UNITY_EDITOR
        return Get_UUID_By_KeyChain();
#else
        return string.Empty;
#endif
    }

    public string saveDeiveIDToKeychain(string deviceID)
    {
#if UNITY_IOS && !UNITY_EDITOR
        return Save_UUID_To_KeyChain(deviceID);
#else
        return string.Empty;
#endif

    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string Get_UUID_By_KeyChain();

    [DllImport("__Internal")]
    private static extern string Save_UUID_To_KeyChain(string deviceID);
#endif
}
