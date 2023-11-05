using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using CommonService;
using Game.Common;
using CommonILRuntime.BindingModule;
using CommonILRuntime.Extension;
using LobbyLogic.Audio;
using System.Threading;

namespace Game.Slot
{

    public class SuperFreeGameState : SlotGameState
    {
		public static Action audioResetBGM;
		protected bool isPlayingBGM = false;


		public SuperFreeGameState(SlotGameBase currentGame, bool isEnter = false) : base(currentGame,isEnter)
        {
            Debug.LogWarning("<<< SuperFreeGameState Game >>>");
        }

        protected override void initState()
		{
			DataStore.getInstance.gameToLobbyService.updateGameStataSubject.OnNext(GameConfig.GameState.SFG);
			if (slotGame.superFreeGameIdx != 0)
            {
				resetTableState();
				return;
			}

			showSuperFreeGameStartWindows(resetTableState);
		}

		protected virtual void showSuperFreeGameStartWindows(Action changestate)
		{ }

		protected void resetTableState()
		{
			gameUI.bottomBarPresenter.setAsSpinWithNoLongPressBtn();
			initTable();
		}

		protected virtual void initTable()
		{
			gameUI.setAsSuperFreeTable();
			slotGame.onSpuerFreeAutoItem();
			setupCallback();
			slotGame.showSuperFreeTable();
			gameUI.setSideText(-1);
			slotGame.setSFGText(1);
			slotGame.openSFGBackground();
			slotGame.setPlayerGameStateAndSubject(GameConfig.PlayerState.SFGSpin);
			CoroutineManager.StartCoroutine(delayFirstSpin());
		}

		protected IEnumerator delayFirstSpin()
		{
			yield return CoroutineManager.StartCoroutine(beforeFirstSpinAction());
			gameUI.setPlayBtnEnable(true);
		}

		protected virtual IEnumerator beforeFirstSpinAction()
		{
			var delayTime = isEnter ? SlotGameBase.gameConfig.ENTER_DELAY_TIME : SlotGameBase.gameConfig.FREE_CUT_SCENE_TIME;
			var delaySpineTime = delayTime + SlotGameBase.gameConfig.NO_WIN_NEXT_SPIN_TIME;
			CoroutineManager.AddCorotuine(delaySpin(delaySpineTime, true));
			yield return delayTime;
		}

		protected override void setupCallback()
		{
			base.setupCallback();
			slotGame.OnSpinHandler = onSpinClick;
			slotGame.OnSuperFreeSpinResult = onReceiveSpinResult;
			slotGame.OnTableSpinEnd = onTableEnd;
			slotGame.OnNormalEndResult = onNormalEnd;
			slotGame.OnSuperFreeEndResult = onFreeEnd;
		}

		protected void onSpinClick()
		{
			if (!isPlayingBGM)
			{
				isPlayingBGM = true;
				audioResetBGM?.Invoke();
			}
			CoroutineManager.StopCorotuine(delaySpin(0f));
			prepareSpin();
		}

		protected void prepareSpin()
		{
			gameUI.bottomBarPresenter.setAsStopButton(false);
			gameUI.setPlayBtnEnable(false);
			doSpin();
		}

		protected virtual void doSpin()
		{
			gameUI.preSpinTable();
			slotGame.sendSuperFreeGameSpin();
		}

		protected override void onSpinResult()
		{
			gameUI.spinTable();
			slotGame.onSpuerFreeAutoItem();
		}

		protected override void doStop()
		{
			gameUI.stopTable();
		}

		protected void onTableEnd()
		{
			slotGame.sendSuperFreeGameEnd();
		}

		protected virtual void onFreeEnd()
		{
			switch (slotGame.NowPlayerState)
			{
				case GameConfig.PlayerState.NGEndFromSFG:
					{
						slotGame.showNiceWinWindow(slotGame.currentWin(), () => {
							float delayTime = getDelaySpinTime();
							CoroutineManager.StartCoroutine(delayEnd(delayTime));
						});
					}
					break;
				case GameConfig.PlayerState.SFGSpin:
					{
						var winInfo = slotGame.getWinInfo();
						var totalWin = winInfo.Total_Win;

						slotGame.setSFGText(0);
						slotGame.showNiceWinWindow(totalWin.ConvertToUlong(), startDelaySpin);
					}
					break;
			}
		}

		protected IEnumerator delayEnd(float delayTime)
		{
			yield return delayTime;
			slotGame.sendNormalGameEnd(GameConfig.PlayerState.NGEndFromSFG);
		}

		protected virtual void onNormalEnd()
		{
			AudioManager.instance.stopBGM();

			gameUI.setTotalWin(0);
			slotGame.changeState(new NormalGameState(slotGame));
			slotGame.superFreeGameTotalCount = 0;
			slotGame.superFreeGameIdx = 0;
		}

		protected void startDelaySpin()
		{
			var delayTime = getDelaySpinTime();
			gameUI.bottomBarPresenter.setAsSpinWithNoLongPressBtn();
			gameUI.setPlayBtnEnable(true);
			CoroutineManager.AddCorotuine(delaySpin(delayTime));
		}

		protected virtual float getDelaySpinTime()
		{
			return 0.5f;
		}

		protected IEnumerator delaySpin(float delayTime, bool isFirstSpin = false)
		{
			yield return delayTime;
			if (isFirstSpin)
			{
				audioResetBGM?.Invoke();
			}
			prepareSpin();
		}
	}
}
