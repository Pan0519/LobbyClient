using Network;
using System.Threading.Tasks;
using LobbyLogic.NetWork.ResponseStruct;
using LobbyLogic.NetWork.RequestStruce;
using EventActivity;

namespace LobbyLogic.NetWork
{
    public class EventServer
    {
        string apiActivity { get { return "/activity"; } }
        ServerProxy proxy;

        EventClickGameSendChoice eventClickClass(int playIndex, int clickItem)
        {
            return new EventClickGameSendChoice() { PlayIndex = playIndex, ClickItem = clickItem };
        }

        public void clientDisconnect()
        {
            throw new System.NotImplementedException();
        }
        public EventServer setServerProxy(ServerProxy serverProxy)
        {
            proxy = serverProxy;
            return this;
        }

        string eventAPI(string api)
        {
            return $"{apiActivity}/{api}/{ActivityDataStore.nowActivityInfo.activityId}";
        }

        public Task<SendSelectBaseResponse> sendSelectItem(int playIndex, int clickItem)
        {
            return proxy.callApi<SendSelectBaseResponse>(eventAPI("play"), eventClickClass(playIndex, clickItem));
        }

        public Task<AppleFrameSelectResponse> sendFrameSelectItem(int playIndex, int clickItem)
        {
            return proxy.callApi<AppleFrameSelectResponse>(eventAPI("play"), eventClickClass(playIndex, clickItem));
        }

        public Task<JourneyPlayResponse> sendFrenzyJourneyPlay(int playID = 0)
        {
            return proxy.callApi<JourneyPlayResponse>(eventAPI("play"), new FrenzyJourneyPlayData() { PlayIndex = playID });
        }
        public Task<BaseInitActivityResponse> getBaseActivityInfo()
        {
            return proxy.callApi<BaseInitActivityResponse>(eventAPI("info"), new ActivityRequestBase());
        }
        public Task<AppleFarmInitResponse> getAppleFarmInitData()
        {
            return proxy.callApi<AppleFarmInitResponse>(eventAPI("info"), new ActivityRequestBase());
        }

        public Task<AppleFarmBoxResponse> sendAppleOpenBox(int clickItem)
        {
            return proxy.callApi<AppleFarmBoxResponse>(eventAPI("box"), new OpenEventBox() { ClickItem = clickItem });
        }

        public Task<BoxResponse> sendOpenBox(int clickItem)
        {
            return proxy.callApi<BoxResponse>(eventAPI("box"), new OpenEventBox() { ClickItem = clickItem });
        }

        public Task<JourneyBossPlayResponse> sendBossPlay(int playID = 0)
        {
            return proxy.callApi<JourneyBossPlayResponse>(eventAPI("boss"), new FrenzyJourneyPlayData() { PlayIndex = playID });
        }

        public Task<JourneyInitResponse> getFrenzyJourneyData()
        {
            return proxy.callApi<JourneyInitResponse>(eventAPI("info"), new ActivityRequestBase());
        }

        public Task<MagicForestInitResponse> getForestInitData()
        {
            return proxy.callApi<MagicForestInitResponse>(eventAPI("info"), new ActivityRequestBase());
        }

        public Task<MagicForestPlayResponse> sendForestPlay(int clickItem, int playIndex = 0)
        {
            return proxy.callApi<MagicForestPlayResponse>(eventAPI("play"), eventClickClass(playIndex, clickItem));
        }

        public Task<MagicForestBossPlayResponse> sendBossPlay(int clickItem, int playIndex = 0)
        {
            return proxy.callApi<MagicForestBossPlayResponse>(eventAPI("boss"), eventClickClass(playIndex, clickItem));
        }

        public Task<MagicForestBossUseResponse> sendBossUse(string useItemName)
        {
            return proxy.callApi<MagicForestBossUseResponse>(eventAPI("use"), new BossUseItem() { PlayIndex = 0, ItemName = useItemName });
        }
    }
}
