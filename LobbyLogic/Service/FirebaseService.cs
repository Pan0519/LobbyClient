using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LitJson;
using UniRx;
using System.Threading.Tasks;
using Debug = UnityLogUtility.Debug;

namespace Service
{
    public static class FirebaseService
    {
        public static Subject<string> TokenSubject = new Subject<string>();
        public static Subject<bool> InitFirebaseSubject = new Subject<bool>();
        public static Subject<bool> IsSendMailVerification = new Subject<bool>();

        static List<string> _fbPermissions;

        static List<string> fbPermissions
        {
            get
            {
                if (null == _fbPermissions)
                {
                    _fbPermissions = new List<string>() { "public_profile" };
                }
                return _fbPermissions;
            }
        }

        static void setSDKCallback()
        {
            FirebaseSDK.instance.initFBCallback = initFBSuccess;
            FirebaseSDK.instance.getAccessTokenSuccess = handleAccessToken;
            FirebaseSDK.instance.sendEmailVerification = sendMailVerification;
        }

        public static void initFB()
        {
            FirebaseSDK.instance.initFB();
        }

        public static void linkFB()
        {
            FirebaseSDK.instance.initFBCallback = linkFBInitSuccess;
            initFB();
        }

        public static void loginWithApple(string appleToken)
        {
            FirebaseSDK.instance.loginWithApple(appleToken);
        }

        public static Task initFirebase()
        {
            setSDKCallback();
            return FirebaseSDK.instance.initFirebase();
        }

        public static void signInWithCustomToken(string token)
        {
            FirebaseSDK.instance.SignInWithCustomToken(token);
        }

        public static void autoLogin()
        {
            FirebaseSDK.instance.AutoLogin();
        }

        public static void logout()
        {
            FirebaseSDK.instance.logout();
        }

        public static bool isAlreadyLogin()
        {
            return FirebaseSDK.instance.isAlreadyLogin;
        }

        public static void updateEmail(string mail)
        {
            FirebaseSDK.instance.FirebaseUpdateEmail(mail);
        }

        static void sendMailVerification(bool isSuccess)
        {
            IsSendMailVerification.OnNext(isSuccess);
        }

        static void linkFBInitSuccess()
        {
            FirebaseSDK.instance.linkFBPermissions(fbPermissions);
        }

        static void initFBSuccess()
        {
            FirebaseSDK.instance.loginFBWithPermissions(fbPermissions);
        }
    
        static void handleAccessToken(string token)
        {
            TokenSubject.OnNext(token);
        }

        static void initFirebaseCallback(bool isLogin)
        {
            InitFirebaseSubject.OnNext(isLogin);
        }
    }
}
