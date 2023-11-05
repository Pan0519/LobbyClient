using UniRx;

namespace Services
{
    public class LobbyToGameServices
    {
        public Subject<bool> cancelAutoSubject = new Subject<bool>();

        public void cancelCurrentAutoPlay()
        {
            cancelAutoSubject.OnNext(true);
        }
    }
}
