using UniRx;
using LobbyLogic.NetWork.ResponseStruct;

namespace FarmBlast
{
    public class FarmBlastDataManager
    {
        static FarmBlastDataManager instance = new FarmBlastDataManager();
        public static FarmBlastDataManager getInstance { get { return instance; } }

        public Subject<BoostsData> boostDataUpdateSub = new Subject<BoostsData>();

        public void updateBoostData(BoostsData data)
        {
            boostDataUpdateSub.OnNext(data);
        }
    }
}
