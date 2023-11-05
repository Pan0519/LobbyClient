using CommonILRuntime.Module;
using UnityEngine;

namespace Game.Common
{
    public class CutScenePresenter : ContainerPresenter
    {
        public override string objPath { get { return "prefab/slot/free_game_cut"; } }
        public override UiLayer uiLayer { get { return UiLayer.GameMessage; } }

        public virtual string animatorDataName { get { return ""; } }
        public override void initUIs()
        {
            cutAnimator = getAnimatorData(animatorDataName);
        }

        Animator cutAnimator;

        public float animationTimes
        {
            get
            {
                return cutAnimator.runtimeAnimatorController.animationClips[0].length;
            }
        }
    }
}