using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class ExtensionMethods
{
    public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOP)
    {
        var tcs = new TaskCompletionSource<object>();
        asyncOP.completed += obj => { tcs.SetResult(null); };
        return ((Task)tcs.Task).GetAwaiter();
    }

    public static TaskAwaiter<UnityWebRequest.Result> GetAwaiter(this UnityWebRequestAsyncOperation asyncOP)
    {
        var tsc = new TaskCompletionSource<UnityWebRequest.Result>();
        asyncOP.completed += async => tsc.TrySetResult(asyncOP.webRequest.result);
        //if (asyncOP.isDone)
        //{
        //    tsc.TrySetResult(asyncOP.webRequest.result);
        //}
        return tsc.Task.GetAwaiter();
    }
}
