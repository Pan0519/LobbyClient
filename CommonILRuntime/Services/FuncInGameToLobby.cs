using UniRx;
using System;
using Debug = UnityLogUtility.Debug;
using System.Collections.Generic;

namespace Services
{
    /// <summary>
    /// Common 的通道，主場呼叫使用大廳系統的橋樑，透過事件觸發，連動LobbyIL domain 所註冊的對應系統
    /// 參考: FuncInGameService.initEventPresenter 
    /// 緣由: GameIL, LobbyIL 分別屬於不同的IL appDomain，兩個Domain間溝通須透過編譯時雙方都有關聯的CommonIL做為橋樑，
    /// 又為了保有 LobbyIL 與 CommonIL 的獨立性(不把所有主場會需要的LobbyIL系統都搬到CommonIL下)，故產生此通道流程
    /// </summary>
    public class FuncInGameToLobbyService
    {
        public Subject<PlatformFuncInfo> eventOpenSubscribe = new Subject<PlatformFuncInfo>();
        public Subject<FunctionNo> eventEndSubscribe = new Subject<FunctionNo>();

        /// <summary>
        /// Common提供給遊戲主場呼叫大廳系統的介面
        /// </summary>
        /// <param name="eventID"></param>
        /// <param name="data"></param>
        public void OpenFuncInLobby(FunctionNo eventID, object data = null)
        {
            eventOpenSubscribe.OnNext(new PlatformFuncInfo(eventID) { Data = data });
        }

        /// <summary>
        /// Common提供給遊戲主場呼叫大廳系統的介面
        /// </summary>
        /// <param name="info"></param>
        public void OpenFuncInLobby(PlatformFuncInfo info)
        {
            eventOpenSubscribe.OnNext(info);
        }

        public void SendEventEnd(FunctionNo functionNo)
        {
            eventEndSubscribe.OnNext(functionNo);
        }
    }

    public class PlatformFuncInfo
    {
        public FunctionNo FunctionID { get; private set; }
        public object Data;

        public PlatformFuncInfo(FunctionNo funcID)
        {
            FunctionID = funcID;
        }
    }

    public enum FunctionNo
    {
        ClearAllDispose = -1,
        Shop,
        LimitShop,
        GoldenEgg,
        SettingPage,
        DailyMission,
        HighRollerVault,
        UpdateRollerVaultPay,
        UpdateDailyMission,
        UpdateAdventureMission,
        ExtraGameLevelComplete,
        OpenTransitionXParty,
        OpenCommonLoadingUi,
        NoCoinNotice,
        EventStart = 20000,
    }
}
