using Debug = UnityLogUtility.Debug;
using UnityEngine.UI;
using System.Collections.Generic;
using CommonService;
using System;
using CommonPresenter;
using System.Threading.Tasks;
using LobbyLogic.Audio;

namespace Game.Slot
{
    /// <summary>
    /// 押注解鎖管理器
    /// </summary>
    public class BetToLockManager
    {
        /// <summary> 該新手保護等級前不鎖任何功能 </summary>
        public const int NOVICE_UNLOCK_LEVEL = 20;

        /// <summary> 新手不鎖任何功能 </summary>
        bool isNovice { get { return DataStore.getInstance.playerInfo.level <= NOVICE_UNLOCK_LEVEL; } }

        /// <summary> 富豪廳不鎖任何功能 </summary>
        bool isHighRoller { get { return DataStore.getInstance.dataInfo.chooseBetClass.Type == ChooseBetClass.High_Roller; } }

        Action<bool, float> OnInitUnlockOrLock;
        Action<bool, float> OnCheckUnlockOrLock;
        Action OnBtnClicked;
        Action OnBtnDisabled;

        /// <summary> 押注資料 </summary>
        List<GameBetInfo> betList;
        /// <summary> 最後的押注百分比(真實) </summary>
        float lastPercent = 0;

        GameBottomBarPresenter bottomBarUI = null;
        /// <summary> 是否要跑初始化? </summary>
        bool isInit = true;
        /// <summary> 是否為暫時檔位? </summary>
        bool isTemporary = false;

        public void init(GameBottomBarPresenter bottomBar)
        {
            bottomBarUI = bottomBar;
            initBetList();
        }

        async void initBetList()
        {
            if (isNovice)
            {
                betList = await DataStore.getInstance.dataInfo.getRegularBetDataInfos(NOVICE_UNLOCK_LEVEL);
                return;
            }
            betList = await DataStore.getInstance.dataInfo.getNowRegularBetDataInfoList();
        }

        /// <summary>
        /// 關掉/開啟 按鈕可按
        /// </summary>
        public void setBtnClick(bool isClick)
        {
            if (isClick)
            {
                OnBtnClicked?.Invoke();
                return;
            }
            OnBtnDisabled?.Invoke();
        }

        /// <summary>
        /// 初始化受押注影響的鎖定功能
        /// </summary>
        public void initUnlockOrLock()
        {
            doInitUnlockOrLock(lastPercent, false);
        }

        /// <summary>
        /// 實作 初始化受押注影響的鎖定功能
        /// </summary>
        /// <param name="isTemporaryOpened"> 斷線重連的廳館是否為富豪廳 </param>
        void doInitUnlockOrLock(float percent, bool isTemporaryHighRoller)
        {
            if (isTemporary)
            {
                OnInitUnlockOrLock?.Invoke(isNovice || isTemporaryHighRoller, percent);
            }
            else
            {
                OnInitUnlockOrLock?.Invoke(isNovice || isHighRoller, percent);
            }
        }

        /// <summary>
        /// 判斷受押注影響的鎖定功能
        /// </summary>
        public void checkUnlockOrLock(float percent)
        {
            lastPercent = percent;
            if (isInit)
            {
                isInit = false;
                initUnlockOrLock();
                return;
            }
            OnCheckUnlockOrLock?.Invoke(isNovice || isHighRoller, lastPercent);
        }

        /// <summary>
        /// 設定該等級的暫時檔位，含其他JP和其他功能的解鎖顯示
        /// </summary>
        /// <param name="betIndex"> 當時押注</param>
        /// <param name="Level"> 當時等級 </param>
        /// <param name="hall"> 斷線重連的廳館</param>

        public async Task<ulong> temporaryBetIndex(int betIndex, int Level, string hall = null)
        {
            isTemporary = true;

            // 無斷線重連的廳館資訊，改取得當前玩家的廳館資訊
            if (string.IsNullOrEmpty(hall))
            {
                hall = DataStore.getInstance.dataInfo.chooseBetClass.Type;
            }

            Dictionary<string, BetBase> betBaseGame = await DataStore.getInstance.dataInfo.getGameBetBase();
            List<GameBetInfo> nowgamebetlist = null;
            string betBaseName = hall;
            if (hall == ChooseBetClass.High_Roller)
            {
                nowgamebetlist = await DataStore.getInstance.dataInfo.getHighRollerBetDataInfoList(Level);
            }
            else
            {
                betBaseName = ChooseBetClass.Regular;
                nowgamebetlist = await DataStore.getInstance.dataInfo.getRegularBetDataInfos(Level);
            }

            BetBase betBase = betBaseGame[betBaseName];
            float betDivide = betBase.percent * nowgamebetlist.Count * 0.01f;
            int betAmount = betBase.upAmount;
            if (betIndex >= betDivide)
            {
                betAmount = betBase.downAmount;
            }

            float percent = 0f;
            ulong showBet = 0;
            int index = 0;
            for (int i = 0; i < nowgamebetlist.Count; i++)
            {
                if (nowgamebetlist[i].totalBetID == betIndex)
                {
                    index = i;
                    percent = (float)(i + 1) / nowgamebetlist.Count;
                    showBet = (ulong)(nowgamebetlist[i].bet * betAmount);
                    break;
                }
            }
            Debug.Log($"非NG當時的總押注檔位:{betIndex} 當時的等級:{Level} 廳館:{hall} 取押注:{betBaseName} 計算出來的當時百分比:{percent} 押注金額是:{showBet} 等級檔位序:{index}");

            // 百分比不可能為零，代表發生錯誤，印出錯誤LOG方便追查問題
            if (percent == 0)
            {
                for (int i = 0; i < nowgamebetlist.Count; i++)
                {
                    Debug.LogError($"序:{i} totalBetID:{nowgamebetlist[i].totalBetID} bet:{nowgamebetlist[i].bet}");
                }
            }

            bottomBarUI.setBetNumTxt(showBet);
            doInitUnlockOrLock(percent, (hall == ChooseBetClass.High_Roller));

            return showBet;
        }

        /// <summary>
        /// 如果有設定過暫時檔位，那就恢復原本的玩家檔位
        /// </summary>
        public void checkNowBetIndex()
        {
            if (isTemporary)
            {
                bottomBarUI.assignNGBetID(DataStore.getInstance.dataInfo.chooseBetClass.BetId);
                isTemporary = false;
            }
        }

        /// <summary>
        /// 加入 受押注影響的按鈕UI
        /// </summary>
        /// <param name="button">按扭</param>
        /// <param name="limitPercent">填入解鎖條件百分比(0~100)</param>
        /// <param name="onLock">鎖住表演</param>
        /// <param name="onUnlock">解鎖表演</param>
        /// <param name="onDefaultLock">鎖住狀態</param>
        /// <param name="onDefaultUnlock">解鎖狀態</param>
        /// <param name="isClick">按鈕是否可按</param>
        public void addLockButton(Button button, int limitPercent, Action onLock, Action onUnlock, Action onDefaultLock = null, Action onDefaultUnlock = null, bool isClick = true)
        {
            if (limitPercent < 0 || limitPercent > 100)
            {
                Debug.LogError($"Unlock button input limit percent({limitPercent}) is illegal.");
                return;
            }

            float betPercentLimit = (float)limitPercent / 100.0f;

            OnInitUnlockOrLock += (bOpened, betPercent) =>
            {
                bool isUnlock = bOpened ? true : (betPercent >= betPercentLimit);
                if (isUnlock)
                    onDefaultUnlock?.Invoke();
                else
                    onDefaultLock?.Invoke();
                button.enabled = !isUnlock;
            };

            OnCheckUnlockOrLock += (bOpened, betPercent) =>
            {
                bool isUnlock = bOpened ? true : (betPercent >= betPercentLimit);
                //Debug.Log($"isUnlock: {isUnlock} , betPercent: {betPercent} , betPercentLimit: {betPercentLimit}");
                //要鎖住而現在未鎖住
                if (false == isUnlock && false == button.enabled)
                {
                    AudioManager.instance.stopOnceAudio();
                    AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(MainGameCommonSound.LockBtn));
                    button.enabled = !isUnlock;
                    onLock();
                }
                //要解鎖而現在鎖住
                if (isUnlock && button.enabled)
                {
                    AudioManager.instance.stopOnceAudio();
                    AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(MainGameCommonSound.UnlockBtn));
                    button.enabled = !isUnlock;
                    onUnlock();
                }
            };

            OnBtnClicked += () =>
            {
                button.interactable = true;
            };

            OnBtnDisabled += () =>
            {
                button.interactable = false;
            };

            if (isClick)
            {
                button.onClick.AddListener(() =>
                {
                    changeBet(limitPercent);
                });
            }

            //特別註記：button.enabled用來紀錄"按鈕是否解鎖"、button.interactable用來記錄"按鈕是否可按(spin時不可按)"
        }

        void changeBet(int limitPercent)
        {
            int Idx = (int)Math.Round((double)betList.Count * limitPercent / 100);
            if (Idx > 0)
            {
                //index從0數起，故-1
                Idx--;
            }
            bottomBarUI.assignNGBetID(Idx);
        }
    }
}
