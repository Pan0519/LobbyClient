using CommonILRuntime.Outcome;
using CommonService;
using Services;
using System.Collections.Generic;
using UniRx;

namespace CommonILRuntime.Services
{
    public class ExtraGameServices
    {
        List<int> completedLevelIDs;
        public Subject<CommonReward[]> levelRewardSubject = new Subject<CommonReward[]>();
        public Subject<bool> gameSwitchSubject = new Subject<bool>();
        public Subject<bool> playBtnEnableSubject = new Subject<bool>();
        public int levelID { get; set; }
        public string characterType { get; set; } = "dog";

        public ExtraGameServices()
        {
            completedLevelIDs = new List<int>();
        }

        public void setCompletedLevel(int level)
        {
            if (!checkHaveCompletedLevel(level))
            {
                completedLevelIDs.Add(level);
            }
            DataStore.getInstance.eventInGameToLobbyService.OpenFuncInLobby(FunctionNo.ExtraGameLevelComplete);
        }

        public bool checkHaveCompletedLevel(int level)
        {
            return completedLevelIDs.Contains(level);
        }
    }
}
