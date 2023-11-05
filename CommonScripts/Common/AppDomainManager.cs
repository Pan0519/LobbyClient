using System.Threading.Tasks;
using AppDomain = ILRuntime.Runtime.Enviorment.AppDomain;

public class AppDomainManager
{
    AppDomain appDomain;
    string gameName { get; set; } = string.Empty;

    string CONTENT_HOST
    {
        get
        {
#if UNITY_EDITOR && STREAMING_ASSETS
            return ApplicationConfig.getStreamingPath;
#else
            //#if UNITY_EDITOR_OSX || UNITY_IOS
            //            return ApplicationConfig.getStreamingPath;
            //#else
            return (!string.IsNullOrEmpty(ApplicationConfig.CONTENT_HOST)) ? ApplicationConfig.CONTENT_HOST : "http://192.168.128.235:8080";
#endif
        }
    }

    //string getiOSHost()
    //{
    //    if (ApplicationConfig.environment == ApplicationConfig.Environment.Dev)
    //    {
    //        return (!string.IsNullOrEmpty(ApplicationConfig.CONTENT_HOST)) ? ApplicationConfig.CONTENT_HOST : "http://192.168.128.235:8080";
    //    }
    //    return 
    //}

    public async Task<AppDomainManager> domainInit(string gameName)
    {
        appDomain = await ILRuntimeManager.instance.init(gameName, CONTENT_HOST);
        this.gameName = gameName;
        if (null == appDomain)
        {
            Util.LogError("ILRuntimeManager init fail. AppDomain is null.");
        }
#if UNITY_EDITOR
        appDomain.DebugService.StartDebugService(56000);
#endif
        return this;
    }

    public void invokeLogicMain(params object[] objects)
    {
        invokeLogicMainMethod("initAsync", objects);
    }

    public void invokeLogicMainMethod(string method, params object[] objects)
    {
        invokeMethod($"{gameName}.LogicMain", method, objects);
    }

    public void invokeMethod(string type, string method, params object[] objects)
    {
        if (null == appDomain)
        {
            Util.LogError($"{type} invokeMethod {method} is Error, appDomain is null");
            return;
        }

        appDomain.Invoke(type, method, null, objects);
    }
}
