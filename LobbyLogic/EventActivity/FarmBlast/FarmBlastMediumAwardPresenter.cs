using UnityEngine;
using System.Collections.Generic;

namespace FarmBlast
{
    class FarmBlastMediumAwardPresenter : FarmBlastAwardPresenter
    {
        public override string objPath
        {
            get { return "prefab/activity/farm_blast/fb_medium_prize"; }
        }

        GameObject plusObj;

        public override void initUIs()
        {
            base.initUIs();
            plusObj = getGameObjectData("plus_obj");
        }

        public override void changePuzzleIcon(List<long> puzzlePackID, string rewardPackID)
        {
            plusObj.setActiveWhenChange(puzzlePackID.Count > 0);
            base.changePuzzleIcon(puzzlePackID, rewardPackID);
        }
    }
}
