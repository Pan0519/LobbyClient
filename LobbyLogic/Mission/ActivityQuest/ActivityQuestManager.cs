using CommonILRuntime.BindingModule;
using Mission;
using SaveTheDog;
using Service;
using System.Collections;
using UniRx;
using UnityEngine;

namespace Mission
{
    public class ActivityQuestManager : MonoBehaviour
    {
        public static ActivityQuestManager instance = new ActivityQuestManager();
        public Subject<bool> closeQuestProgress = new Subject<bool>();
        public async void onQuestComplete()
        {
            var redeem = await AppManager.lobbyServer.getNewbieAdventureRedeem();
            SaveTheDogMapData.instance.updateAdventureRecord(redeem.adventureRecord);
            var rewardPack = await AppManager.lobbyServer.getRewardPacks(redeem.rewardPackId);
            UiManager.getPresenter<ActivityQuestRewardPresenter>().getRewardInfo(rewardPack.rewards);
            ActivityQuestData.isRewardCanShow = false;
            closeQuestProgress.OnNext(true); 
        }
    }
}