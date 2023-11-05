using CommonILRuntime.Module;
using EventActivity;
using UnityEngine.UI;
using UnityEngine;
using UniRx;

namespace MagicForest
{
    public class RewardTicketNode : NodePresenter
    {
        Text ticketNum;

        public override void initUIs()
        {
            ticketNum = getTextData("reward_ticket_text");
        }

        public void updateTicketNum(long ticketCount)
        {
            ticketNum.text = ticketCount <= 99 ? ticketCount.ToString() : "99+";
        }
    }

    public class TicketWithAnim : RewardTicketNode
    {
        Animator ticketAnim;

        public override void initUIs()
        {
            base.initUIs();
            ticketAnim = getAnimatorData("anim_ticket");
        }

        public override void init()
        {
            ForestDataServices.totalTicketSub.Subscribe(updateTicketNum).AddTo(uiGameObject);
        }

        public void playGetAnim()
        {
            ticketAnim.SetTrigger("get");
        }
    }
}
