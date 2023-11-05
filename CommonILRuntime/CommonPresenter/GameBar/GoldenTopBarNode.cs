using UnityEngine;
using UnityEngine.UI;
using LobbyLogic.Audio;
using CommonService;
using CommonILRuntime.Module;

namespace CommonPresenter
{
    public class GoldenTopBarNode : NodePresenter
    {
        Button goldenEggBtn;
        GameObject eggMaxObj;
        Animator statausAnim;
        public override void initUIs()
        {
            goldenEggBtn = getBtnData("golden_egg_btn");
            eggMaxObj = getGameObjectData("egg_max");
            statausAnim = getAnimatorData("status_anim");
        }

        public override void init()
        {
            eggMaxObj.setActiveWhenChange(false);
            goldenEggBtn.onClick.AddListener(openGoldenEgg);
        }

        public virtual void openGoldenEgg()
        {
            AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.InfoBtn));
        }
        public void setEggMaxActive(bool active)
        {
            eggMaxObj.setActiveWhenChange(active);
            string maxStatusTriggerName = active ? "loop" : "nor";
            statusAnimPlay(maxStatusTriggerName);
        }
        public void statusAnimPlay(string triggerName)
        {
            statausAnim.SetTrigger(triggerName);
        }
    }
}
