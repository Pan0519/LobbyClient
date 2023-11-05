using EventActivity;
using UnityEngine;
using System.Collections.Generic;
using Lobby.Jigsaw;
using UniRx;
using System;
using Common.Jigsaw;
using CommonILRuntime.Outcome;
using Service;
using CommonILRuntime.BindingModule;
using CommonPresenter.PackItem;
using Debug = UnityLogUtility.Debug;

namespace FarmBlast
{
    public class FarmBlastAwardPresenter : AwardBasePresenter
    {
        List<long> packIDs;
        //int openPackId = 0;
        //IDisposable openPackDis;
        public virtual float packScale { get; set; }

        RectTransform packGroupTrans;

        string rewardPackID;
        public override void initContainerPresenter()
        {
            resOrder = new string[] { AssetBundleData.getBundleName(BundleType.FarmBlast)};
            base.initContainerPresenter();
        }
        public override void initUIs()
        {
            packGroupTrans = getBindingData<RectTransform>("reward_pack_group");
            base.initUIs();
        }

        public override void init()
        {
            base.init();
            packGroupTrans.gameObject.setActiveWhenChange(true);
        }

        public virtual void changePuzzleIcon(List<long> packIDs, string rewardPackID)
        {
            if (packIDs.Count <= 0)
            {
                //Debug.Log($"changePuzzleIcon count <=0");
                return;
            }

            this.packIDs = packIDs;
            this.rewardPackID = rewardPackID;
            PackItemPresenterServices.getPickItems(packIDs, packGroupTrans);
        }

        public override void animOut()
        {
            if (null == packIDs)
            {
                base.animOut();
                return;
            }
            openPieceData();
        }

        void openPieceData()
        {
            packIDs = null;
            OpenPackWildProcess.openPackWildFromID(rewardPackID, animOut);
        }
    }
}
