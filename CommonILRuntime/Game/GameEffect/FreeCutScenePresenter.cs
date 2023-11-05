using CommonILRuntime.Module;
using UnityEngine;
using Game.Slot;

namespace Game.Common
{
    public class FreeCutScenePresenter : CutScenePresenter
    {
        public override string objPath { get { return SlotGameBase.gameConfig.PATH_CUT_SCENE_ANIMATOR_FREE; } }
        public override string animatorDataName { get { return SlotGameBase.gameConfig.CUT_SCENE_ANIMATOR_FREE; } }

    }
}