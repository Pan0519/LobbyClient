using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Services;
using System;
using System.Threading.Tasks;

namespace Common
{
    public class LvTipNodePresenter : NodePresenter
    {
        List<TipNode> directionList = new List<TipNode>();
        TipNode openTipNode = null;

        bool isOpenTipObj;
        IDisposable autoCloseDiamondTipObjDis;

        public override void initUIs()
        {
            directionList.Add(UiManager.bindNode<TipNode>(getNodeData("bottom_node").cachedGameObject));
            directionList.Add(UiManager.bindNode<TipNode>(getNodeData("top_node").cachedGameObject));
            directionList.Add(UiManager.bindNode<TipNode>(getNodeData("left_node").cachedGameObject));
            directionList.Add(UiManager.bindNode<TipNode>(getNodeData("right_node").cachedGameObject));
        }
        public override void init()
        {
            for (int i = 0; i < directionList.Count; ++i)
            {
                directionList[i].close();
            }
        }

        public void openLvTip(LvTipArrowDirection openDirection, int unLockLv)
        {
            if (isOpenTipObj || unLockLv < 0)
            {
                closelvTip();
                return;
            }
            isOpenTipObj = true;
            openTipNode = directionList[(int)openDirection];
            openTipNode.setUnLockLv(unLockLv);
            openTipNode.open();
            autoCloseDiamondTipObjDis = Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ =>
            {
                closelvTip();
            }).AddTo(uiGameObject);
        }

        public void closelvTip()
        {
            if (null == openTipNode)
            {
                return;
            }
            UtilServices.disposeSubscribes(autoCloseDiamondTipObjDis);
            isOpenTipObj = false;
            openTipNode.closeTip();
        }
    }

    class TipNode : NodePresenter
    {
        Animator tipAnim;
        Text unlockLvTxt;
        bool isClosing;
        GameObject zhLvObj;
        GameObject enLvObj;

        RectTransform unLockTxtParent;
        IDisposable closeDis;

        public override void initUIs()
        {
            tipAnim = getAnimatorData("tip_anim");
            unlockLvTxt = getTextData("unlock_lv_txt");
            zhLvObj = getGameObjectData("text_zh_obj");
            enLvObj = getGameObjectData("text_en_obj");
        }

        public override void init()
        {
            isClosing = false;
            unLockTxtParent = unlockLvTxt.transform.parent.GetComponent<RectTransform>();
            zhLvObj.setActiveWhenChange(ApplicationConfig.nowLanguage == ApplicationConfig.Language.ZH);
            enLvObj.setActiveWhenChange(ApplicationConfig.nowLanguage == ApplicationConfig.Language.EN);
        }
        public async void setUnLockLv(int unLockLv)
        {
            unlockLvTxt.text = unLockLv.ToString();
            await Task.Delay(TimeSpan.FromSeconds(0.3f));
            LayoutRebuilder.ForceRebuildLayoutImmediate(unLockTxtParent);
        }

        public override async void open()
        {
            if (isClosing)
            {
                closeDis.Dispose();
                close();
                await Task.Delay(TimeSpan.FromSeconds(0.1f));
            }
            base.open();
        }

        public void closeTip()
        {
            isClosing = true;
            tipAnim.SetTrigger("close");
            closeDis = Observable.TimerFrame(30).Subscribe(_ =>
             {
                 close();
             }).AddTo(uiGameObject);
        }

        public override void close()
        {
            isClosing = false;
            base.close();
        }
    }

    public enum LvTipArrowDirection
    {
        Bottom,
        Top,
        Left,
        Right,
    }
}
