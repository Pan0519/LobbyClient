using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenEgg
{
    class ChooseModelNode : NodePresenter
    {
        GameObject maxObj;
        Animator fingerAnim;

        public override void initUIs()
        {
            maxObj = getGameObjectData("max_obj");
            fingerAnim = getAnimatorData("finger_anim");
        }

        public void setMaxObjEnable(bool isEnable)
        {
            maxObj.setActiveWhenChange(isEnable);
        }

        public void setAnimEnable(bool isEnable)
        {
            fingerAnim.enabled = isEnable;
        }
    }
}
