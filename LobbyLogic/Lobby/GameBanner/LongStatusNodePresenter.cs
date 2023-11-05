using UnityEngine;
using UnityEngine.UI;
using Services;
using Common;
using CommonService;
using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;

namespace Lobby
{
    class LongStatusNodePresenter : NodePresenter
    {
        GameObject jpObj;
        Text jpNumTxt;
        GameObject lockObj;
        LvTipNodePresenter lvTipNode;
        Text gameNameTxt;
        int unLockLv;

        GameObject hintNewObj;
        GameObject hintHotObj;
        public override void initUIs()
        {
            jpObj = getGameObjectData("jp_obj");
            jpNumTxt = getTextData("jp_num_txt");
            lockObj = getGameObjectData("lock_obj");
            gameNameTxt = getTextData("game_name");
            hintNewObj = getGameObjectData("hint_new_obj");
            hintHotObj = getGameObjectData("hint_hot_obj");
            lvTipNode = UiManager.bindNode<LvTipNodePresenter>(getNodeData("unlock_lv_tip_node").cachedGameObject);
        }

        public override void init()
        {
            hintNewObj.setActiveWhenChange(false);
            hintHotObj.setActiveWhenChange(false);
            lockObj.setActiveWhenChange(false);
            gameNameTxt.gameObject.setActiveWhenChange(ApplicationConfig.nowLanguage != ApplicationConfig.Language.EN && ApplicationConfig.nowLanguage != ApplicationConfig.Language.ZH);
        }

        public void setUnLockLv(int unLockLv)
        {
            this.unLockLv = unLockLv;
            lockObj.setActiveWhenChange(DataStore.getInstance.playerInfo.level < unLockLv);
        }

        public void setGameName(string name)
        {
            gameNameTxt.text = name;
        }

        public void openLvTip()
        {
            lvTipNode.openLvTip(LvTipArrowDirection.Left, unLockLv);
        }

        public void setJPObjEnable(bool active)
        {
            jpObj.setActiveWhenChange(active);
        }
        public void updateJPText(long jpUpdateVal)
        {
            jpNumTxt.text = jpUpdateVal.ToString("N0");
        }

        public void setHintObjActivte(GameState gameState)
        {
            hintNewObj.setActiveWhenChange(gameState == GameState.New);
            hintHotObj.setActiveWhenChange(gameState == GameState.Hot);
        }
    }
}
