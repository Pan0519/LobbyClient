namespace CommonILRuntime.Services
{
    public interface ILongValueTweenerHandler
    {
        void onValueChanged(ulong value);
        UnityEngine.GameObject getDisposableObj();
    }
}
