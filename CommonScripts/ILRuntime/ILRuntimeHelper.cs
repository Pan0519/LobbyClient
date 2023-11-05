using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using ILRuntime.Runtime.Adaptors;
using AppDomain = ILRuntime.Runtime.Enviorment.AppDomain;
using DG.Tweening;
public class AppDomainLoadData
{
    public Task<byte[]> loadDll { get; private set; }
    public Task<byte[]> loadPdb { get; private set; }
    public AppDomainLoadData(Task<byte[]> loadDll, Task<byte[]> loadPdb)
    {
        this.loadDll = loadDll;
        this.loadPdb = loadPdb;


    }
}

public class ILRuntimeHelper : Singleton<ILRuntimeHelper>
{
    public async Task<AppDomain> initAppDomain(List<AppDomainLoadData> loadDatas)
    {
        try
        {
            AppDomain appDomain = new AppDomain();

            for (int i = 0; i < loadDatas.Count; ++i)
            {
                var loadData = loadDatas[i];

                byte[] commonDllBytes = await loadData.loadDll;
                MemoryStream fs = new MemoryStream(commonDllBytes);

#if PROD || STAGE
                appDomain.LoadAssembly(fs);
#else
                byte[] commonPdbData = await loadData.loadPdb;
                MemoryStream p = new MemoryStream(commonPdbData);
                appDomain.LoadAssembly(fs, p, new ILRuntime.Mono.Cecil.Pdb.PdbReaderProvider());
#endif

            }

            if (InitializeILRuntime(appDomain))
            {
                return appDomain;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"initAppDomain Error {e.Message}");
        }

        return null;
    }

    public bool InitializeILRuntime(AppDomain appDomain)
    {
        LitJson.JsonMapper.RegisterImporter<ulong, UInt64>((ulong value) =>
        {
            return (UInt64)value;
        });

        registerCrossBindingAdaptor(appDomain);

        lazyRegister<Int64>(appDomain);
        lazyRegister<UInt64>(appDomain);
        lazyRegister<Int32>(appDomain);
        lazyRegister<UInt32>(appDomain);
        lazyRegister<Int16>(appDomain);
        lazyRegister<UInt16>(appDomain);
        lazyRegister<Byte>(appDomain);
        lazyRegister<SByte>(appDomain);
        lazyRegister<Single>(appDomain);
        lazyRegister<Double>(appDomain);
        lazyRegister<String>(appDomain);
        lazyRegister<Char>(appDomain);
        lazyRegister<Boolean>(appDomain);

        lazyRegister<Vector2>(appDomain);
        lazyRegister<Vector3>(appDomain);
        lazyRegister<Vector4>(appDomain);
        lazyRegister<Quaternion>(appDomain);
        lazyRegister<UnityEngine.Object>(appDomain);
        lazyRegister<GameObject>(appDomain);
        lazyRegister<Transform>(appDomain);
        lazyRegister<RectTransform>(appDomain);
        lazyRegister<Component>(appDomain);
        lazyRegister<Behaviour>(appDomain);
        lazyRegister<MonoBehaviour>(appDomain);
        lazyRegister<AudioClip>(appDomain);
        lazyRegister<Animator>(appDomain);
        lazyRegister<Animation>(appDomain);
        lazyRegister<AnimationClip>(appDomain);
        lazyRegister<TextAsset>(appDomain);
        lazyRegister<Texture>(appDomain);
        lazyRegister<Texture2D>(appDomain);
        lazyRegister<Texture2DArray>(appDomain);
        lazyRegister<Texture3D>(appDomain);
        lazyRegister<ParticleSystem>(appDomain);
        lazyRegister<System.Attribute>(appDomain);


        lazyRegister<ILRuntime.Runtime.Intepreter.ILTypeInstance>(appDomain);

        #region DelegateConvertor

        appDomain.DelegateManager.RegisterDelegateConvertor<Predicate<ILRuntime.Runtime.Intepreter.ILTypeInstance>>((act) =>
        {
            return new Predicate<ILRuntime.Runtime.Intepreter.ILTypeInstance>((obj) =>
            {
                return ((Func<ILRuntime.Runtime.Intepreter.ILTypeInstance, bool>)act)(obj);
            });
        });


        // UnityAction convert to Action
        appDomain.DelegateManager.RegisterDelegateConvertor<UnityEngine.Events.UnityAction>((unityAction) =>
        {
            return new UnityEngine.Events.UnityAction(() =>
            {
                ((Action)unityAction)();
            });
        });

        // UnityAction<BaseEventData> convert to Action<BaseEventData>
        appDomain.DelegateManager.RegisterDelegateConvertor<UnityEngine.Events.UnityAction<UnityEngine.EventSystems.BaseEventData>>((unityAction) =>
        {
            return new UnityEngine.Events.UnityAction<UnityEngine.EventSystems.BaseEventData>((data) =>
            {
                ((Action<UnityEngine.EventSystems.BaseEventData>)unityAction)(data);
            });
        });

        // for sort
        appDomain.DelegateManager.RegisterDelegateConvertor<Comparison<ILRuntime.Runtime.Intepreter.ILTypeInstance>>((act) =>
        {
            return new Comparison<ILRuntime.Runtime.Intepreter.ILTypeInstance>((x, y) =>
            {
                return ((Func<ILRuntime.Runtime.Intepreter.ILTypeInstance, ILRuntime.Runtime.Intepreter.ILTypeInstance, Int32>)act)(x, y);
            });
        });

        // for LitJson
        appDomain.DelegateManager.RegisterDelegateConvertor<LitJson.WrapperFactory>((act) =>
        {
            return new LitJson.WrapperFactory(() =>
            {
                return ((Func<LitJson.IJsonWrapper>)act)();
            });
        });


        // for System.Net.Http.HttpClient using https
        appDomain.DelegateManager.RegisterDelegateConvertor<System.Net.Security.RemoteCertificateValidationCallback>((act) =>
        {
            return new System.Net.Security.RemoteCertificateValidationCallback((sender, certificate, chain, sslPolicyErrors) =>
            {
                return ((Func<System.Object, System.Security.Cryptography.X509Certificates.X509Certificate, System.Security.Cryptography.X509Certificates.X509Chain, System.Net.Security.SslPolicyErrors, Boolean>)act)(sender, certificate, chain, sslPolicyErrors);
            });
        });

        appDomain.DelegateManager.RegisterDelegateConvertor<Predicate<UnityEngine.Sprite>>((act) =>
        {
            return new System.Predicate<UnityEngine.Sprite>((obj) =>
            {
                return ((Func<UnityEngine.Sprite, System.Boolean>)act)(obj);
            });
        });

        appDomain.DelegateManager.RegisterDelegateConvertor<Predicate<System.Int64>>((act) =>
        {
            return new System.Predicate<System.Int64>((obj) =>
            {
                return ((Func<System.Int64, System.Boolean>)act)(obj);
            });
        });
        #endregion DelegateConvertor

        #region MethodDelegate
        // for method(BaseEventData)
        appDomain.DelegateManager.RegisterMethodDelegate<UnityEngine.EventSystems.BaseEventData>();

        // for method(PointerEventData)
        appDomain.DelegateManager.RegisterMethodDelegate<UnityEngine.EventSystems.PointerEventData>();

        // for method(IEnumerator)
        appDomain.DelegateManager.RegisterMethodDelegate<System.Collections.IEnumerator>();

        // for method(BinaryReader)
        appDomain.DelegateManager.RegisterMethodDelegate<BinaryReader>();

        // for method(int, bool)
        appDomain.DelegateManager.RegisterMethodDelegate<int, bool>();

        // for method(GameObject, int)
        appDomain.DelegateManager.RegisterMethodDelegate<GameObject, int>();

        // for method(VertexHelper)
        appDomain.DelegateManager.RegisterMethodDelegate<UnityEngine.UI.VertexHelper>();

        // for method(language.subject)
        appDomain.DelegateManager.RegisterMethodDelegate<global::ApplicationConfig.Language>();

        // for method(Services.TimerService)
        appDomain.DelegateManager.RegisterMethodDelegate<IList<int>>();
        //for IAP
        appDomain.DelegateManager.RegisterMethodDelegate<UnityEngine.Purchasing.Product[]>();
        appDomain.DelegateManager.RegisterMethodDelegate<Dictionary<string, Dictionary<string, object>>>();

        appDomain.DelegateManager.RegisterMethodDelegate<Tuple<bool, DateTime>>();

        #endregion MethodDelegate

        #region FunctionDelegate

        // for Task method
        appDomain.DelegateManager.RegisterFunctionDelegate<Task>();

        // for IEnumerator method
        appDomain.DelegateManager.RegisterFunctionDelegate<System.Collections.IEnumerator>();

        // for IJsonWrapper
        appDomain.DelegateManager.RegisterFunctionDelegate<LitJson.IJsonWrapper>();

        // for System.Net.Http.HttpClient using https
        appDomain.DelegateManager.RegisterFunctionDelegate<object, System.Security.Cryptography.X509Certificates.X509Certificate, System.Security.Cryptography.X509Certificates.X509Chain, System.Net.Security.SslPolicyErrors, System.Boolean>();

        //for Sprite Match
        appDomain.DelegateManager.RegisterFunctionDelegate<Sprite, bool>();
        appDomain.DelegateManager.RegisterFunctionDelegate<long, bool>();

        //for Regex.IsMatch
        appDomain.DelegateManager.RegisterFunctionDelegate<System.Text.RegularExpressions.Match, string>();

        // for Services.TimerService
        appDomain.DelegateManager.RegisterFunctionDelegate<int, bool>();

        // for unirx observe animator 
        appDomain.DelegateManager.RegisterFunctionDelegate<UniRx.Triggers.ObservableStateMachineTrigger.OnStateInfo, System.Boolean>();

        #endregion FunctionDelegate

        LitJson.JsonMapper.RegisterILRuntimeCLRRedirection(appDomain);

        //CLRBinding是ILRuntime其他东西都注册完初始化完最后才能执行
        return InitializeILRuntimeCLRBindings(appDomain);
    }

    // 懶人註冊..  不是通用的東西就不要放在這寫
    private void lazyRegister<T>(AppDomain appDomain)
    {
        appDomain.DelegateManager.RegisterFunctionDelegate<T>();
        appDomain.DelegateManager.RegisterFunctionDelegate<T, T>();
        appDomain.DelegateManager.RegisterFunctionDelegate<T[]>();
        appDomain.DelegateManager.RegisterFunctionDelegate<T[], T[]>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Queue<T>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Queue<T[]>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Queue<List<T>>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Queue<List<T[]>>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Stack<T>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Stack<T[]>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Stack<List<T>>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Stack<List<T[]>>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<List<T>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<List<T>, List<T>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<List<T[]>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<List<T[]>, List<T[]>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<LinkedList<T>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<LinkedList<T[]>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Dictionary<T, T>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Dictionary<T, ILRuntime.Runtime.Intepreter.ILTypeInstance>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Dictionary<T, List<ILRuntime.Runtime.Intepreter.ILTypeInstance>>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<HashSet<T>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Tuple<T, T>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Tuple<T, T, T>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Tuple<T, T, T, T>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Tuple<T, T, T, T, T>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Tuple<T[], T[]>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Tuple<T[], T[], T[]>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Tuple<T[], T[], T[], T[]>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Tuple<T[], T[], T[], T[], T[]>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Tuple<List<T>, List<T>>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Tuple<List<T[]>, List<T[]>>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Tuple<Queue<T>, Queue<T>>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Tuple<Queue<T[]>, Queue<T[]>>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Task<T>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Task<T[]>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Task<List<T>>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Task<List<T[]>>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Task<ILRuntime.Runtime.Intepreter.ILTypeInstance>, T>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Task<ILRuntime.Runtime.Intepreter.ILTypeInstance>, T[]>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Task<ILRuntime.Runtime.Intepreter.ILTypeInstance>, List<T>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Task<ILRuntime.Runtime.Intepreter.ILTypeInstance>, List<T[]>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Task<Dictionary<T, T>>>();
        appDomain.DelegateManager.RegisterFunctionDelegate<Task<Dictionary<T, List<ILRuntime.Runtime.Intepreter.ILTypeInstance>>>>();

        appDomain.DelegateManager.RegisterFunctionDelegate<ILRuntime.Runtime.Intepreter.ILTypeInstance, decimal>();
        appDomain.DelegateManager.RegisterFunctionDelegate<ILRuntime.Runtime.Intepreter.ILTypeInstance, bool>();
        appDomain.DelegateManager.RegisterFunctionDelegate<string, bool>();

        #region for sorting
        appDomain.DelegateManager.RegisterFunctionDelegate<ILRuntime.Runtime.Intepreter.ILTypeInstance, T>();
        appDomain.DelegateManager.RegisterFunctionDelegate<ILRuntime.Runtime.Intepreter.ILTypeInstance, ILRuntime.Runtime.Intepreter.ILTypeInstance, T>();
        #endregion

        appDomain.DelegateManager.RegisterMethodDelegate<T>();
        appDomain.DelegateManager.RegisterMethodDelegate<T, T>();
        appDomain.DelegateManager.RegisterMethodDelegate<T[]>();
        appDomain.DelegateManager.RegisterMethodDelegate<T[], T[]>();
        appDomain.DelegateManager.RegisterMethodDelegate<Queue<T>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Queue<T[]>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Queue<List<T>>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Queue<List<T[]>>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Stack<T>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Stack<T[]>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Stack<List<T>>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Stack<List<T[]>>>();
        appDomain.DelegateManager.RegisterMethodDelegate<List<T>>();
        appDomain.DelegateManager.RegisterMethodDelegate<List<T>, List<T>>();
        appDomain.DelegateManager.RegisterMethodDelegate<List<T[]>>();
        appDomain.DelegateManager.RegisterMethodDelegate<List<T[]>, List<T[]>>();
        appDomain.DelegateManager.RegisterMethodDelegate<LinkedList<T>>();
        appDomain.DelegateManager.RegisterMethodDelegate<LinkedList<T[]>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Dictionary<T, T>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Dictionary<T, ILRuntime.Runtime.Intepreter.ILTypeInstance>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Dictionary<T, List<ILRuntime.Runtime.Intepreter.ILTypeInstance>>>();
        appDomain.DelegateManager.RegisterMethodDelegate<HashSet<T>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Tuple<T, T>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Tuple<T, T, T>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Tuple<T, T, T, T>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Tuple<T, T, T, T, T>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Tuple<T[], T[]>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Tuple<T[], T[], T[]>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Tuple<T[], T[], T[], T[]>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Tuple<T[], T[], T[], T[], T[]>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Tuple<List<T>, List<T>>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Tuple<List<T[]>, List<T[]>>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Tuple<Queue<T>, Queue<T>>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Tuple<Queue<T[]>, Queue<T[]>>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Task<T>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Task<T[]>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Task<List<T>>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Task<List<T[]>>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Task<ILRuntime.Runtime.Intepreter.ILTypeInstance>, T>();
        appDomain.DelegateManager.RegisterMethodDelegate<Task<ILRuntime.Runtime.Intepreter.ILTypeInstance>, T[]>();
        appDomain.DelegateManager.RegisterMethodDelegate<Task<ILRuntime.Runtime.Intepreter.ILTypeInstance>, List<T>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Task<ILRuntime.Runtime.Intepreter.ILTypeInstance>, List<T[]>>();
        appDomain.DelegateManager.RegisterMethodDelegate<Sprite>();
        appDomain.DelegateManager.RegisterMethodDelegate<Sprite[]>();
        appDomain.DelegateManager.RegisterMethodDelegate<DateTime>();
        appDomain.DelegateManager.RegisterMethodDelegate<UniRx.Triggers.ObservableStateMachineTrigger.OnStateInfo>();
        appDomain.DelegateManager.RegisterFunctionDelegate<System.IObserver<System.Int64>, System.IDisposable>();

        //appDomain.DelegateManager.RegisterMethodDelegate<global::HttpDownloadHelper.DOWNLOAD_STATUS, Int64, Int64, Single>();

        appDomain.DelegateManager.RegisterDelegateConvertor<UnityEngine.Events.UnityAction<T>>((act) =>
        {
            return new UnityEngine.Events.UnityAction<T>((arg0) =>
            {
                ((Action<T>)act)(arg0);
            });
        });
        appDomain.DelegateManager.RegisterDelegateConvertor<UnityEngine.Events.UnityAction<T[]>>((act) =>
        {
            return new UnityEngine.Events.UnityAction<T[]>((arg0) =>
            {
                ((Action<T[]>)act)(arg0);
            });
        });
        appDomain.DelegateManager.RegisterDelegateConvertor<UnityEngine.Events.UnityAction<List<T>>>((act) =>
        {
            return new UnityEngine.Events.UnityAction<List<T>>((arg0) =>
            {
                ((Action<List<T>>)act)(arg0);
            });
        });
        appDomain.DelegateManager.RegisterDelegateConvertor<UnityEngine.Events.UnityAction<List<T[]>>>((act) =>
        {
            return new UnityEngine.Events.UnityAction<List<T[]>>((arg0) =>
            {
                ((Action<List<T[]>>)act)(arg0);
            });
        });

        appDomain.DelegateManager.RegisterDelegateConvertor<DG.Tweening.TweenCallback>((act) =>
        {
            return new DG.Tweening.TweenCallback(() =>
            {
                ((Action)act)();
            });
        });

        appDomain.DelegateManager.RegisterDelegateConvertor<System.Text.RegularExpressions.MatchEvaluator>((act) =>
        {
            return new System.Text.RegularExpressions.MatchEvaluator((match) =>
            {
                return ((Func<System.Text.RegularExpressions.Match, System.String>)act)(match);
            });
        });

        appDomain.DelegateManager.RegisterDelegateConvertor<TweenModule.RoulatteTurnTable.TurnTableOverNotify>((act) =>
        {
            return new TweenModule.RoulatteTurnTable.TurnTableOverNotify(() =>
            {
                ((Action)act)();
            });
        });

        appDomain.DelegateManager.RegisterDelegateConvertor<TweenModule.RoulatteTurnTable.TurnTableStoppingNotify>((act) =>
        {
            return new TweenModule.RoulatteTurnTable.TurnTableStoppingNotify(() =>
            {
                ((Action)act)();
            });
        });

        appDomain.DelegateManager.RegisterDelegateConvertor<System.Predicate<System.String>>((act) =>
        {
            return new Predicate<string>((obj) =>
            {
                return ((Func<string, bool>)act)(obj);
            });
        });


        appDomain.DelegateManager.RegisterDelegateConvertor<DG.Tweening.TweenCallback>((act) =>
        {
            return new DG.Tweening.TweenCallback(() =>
            {
                ((Action)act)();
            });
        });
        appDomain.DelegateManager.RegisterFunctionDelegate<System.Single>();
        appDomain.DelegateManager.RegisterDelegateConvertor<DG.Tweening.Core.DOGetter<System.Single>>((act) =>
        {
            return new DG.Tweening.Core.DOGetter<System.Single>(() =>
            {
                return ((Func<System.Single>)act)();
            });
        });
        appDomain.DelegateManager.RegisterMethodDelegate<System.Single>();
        appDomain.DelegateManager.RegisterDelegateConvertor<DG.Tweening.Core.DOSetter<System.Single>>((act) =>
        {
            return new DG.Tweening.Core.DOSetter<System.Single>((pNewValue) =>
            {
                ((Action<System.Single>)act)(pNewValue);
            });
        });
    }

    public void registerCrossBindingAdaptor(AppDomain appdomain)
    {
        // for Corutine
        appdomain.RegisterCrossBindingAdaptor(new CoroutineAdapter());

        // for async & await
        appdomain.RegisterCrossBindingAdaptor(new IAsyncStateMachineClassInheritanceAdaptor());

        appdomain.RegisterCrossBindingAdaptor(new IMonoSingletonAdapter());


    }

    public bool InitializeILRuntimeCLRBindings(AppDomain appdomain)
    {
#if UNITY_EDITOR
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        try
        {
            Type type = assembly.GetType("ILRuntime.Runtime.Generated.CLRBindings");
            var method = type.GetMethod("Initialize");
            method.Invoke(null, new object[] { appdomain });
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            Debug.LogError("ILRuntime.Runtime.Generated.CLRBindings.Initialize fail. Please build Dll first and execute MenuItem:ILRuntime/Generate CLR Binding Code by Analysis.");
            return false;
        }

#else
        ILRuntime.Runtime.Generated.CLRBindings.Initialize(appdomain);
         Debug.Log("Generated.CLRBindings");
#endif
        return true;
    }
}
