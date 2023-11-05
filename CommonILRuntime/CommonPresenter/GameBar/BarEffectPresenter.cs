using CommonILRuntime.Module;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace CommonPresenter
{
    public class BarEffectPresenter : NodePresenter
    {
        protected Animator animator = null;
        protected string endStateName = string.Empty;
        protected virtual string animatorId { get { return "total_effect"; } }

        public override void initUIs()
        {
            animator = getAnimatorData(animatorId);
        }

        public void setEndStateName(string endStateName)
        {
            this.endStateName = endStateName;
        }

        public async override void open()
        {
            base.open();
            await waiteAniOver();
            close();
            ResourceManager.instance.returnObjectToPool(uiGameObject);
        }

        private async Task waiteAniOver()
        {
            float endAniLength = 0f;
            while (!tryGetAniLength(animator, endStateName, ref endAniLength))
            {
                await Task.Yield();
            }
            await Task.Delay(TimeSpan.FromSeconds(endAniLength));
        }

        private bool tryGetAniLength(Animator animator, string aniStateName, ref float length)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            string targetStateName = getTargetAniStateName(animator, aniStateName);
            bool isSuccess = state.IsName(targetStateName);

            if (isSuccess)
            {
                length = state.length;
            }

            return isSuccess;
        }

        private string getTargetAniStateName(Animator animator, string aniStateName)
        {
            const string stateNameFormat = "{0}.{1}";
            return string.Format(stateNameFormat, animator.GetLayerName(0), aniStateName);
        }
    }
}
