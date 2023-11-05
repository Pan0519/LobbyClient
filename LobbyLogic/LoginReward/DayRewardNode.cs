using UnityEngine;
using UnityEngine.UI;
using CommonILRuntime.Module;
using UniRx;

namespace LoginReward
{
    class DayRewardNode : NodePresenter
    {
        GameObject increaseObj;
        GameObject goalObj;
        Button infoBtn;
        //Animator getRewardAnim;

        public Subject<DayRewardNode> infoBtnClickSub = new Subject<DayRewardNode>();
        public int goalDayNum { get; private set; }

        public override void initUIs()
        {
            increaseObj = getGameObjectData("increase_obj");
            goalObj = getGameObjectData("goal_obj");
            infoBtn = getBtnData("info_btn");
            //getRewardAnim = getAnimatorData("day_reward_anim");
        }

        public override void init()
        {
            infoBtn.onClick.AddListener(infoBtnClick);
        }

        public void setGoalDayNum(int goalDayNum)
        {
            this.goalDayNum = goalDayNum;
        }

        public void setDayRewardGoal(int nowDayNum)
        {
            bool isGoal = nowDayNum >= goalDayNum;
            increaseObj.setActiveWhenChange(!isGoal);
            goalObj.setActiveWhenChange(isGoal);
        }
        //public void playGetAnim()
        //{
        //    getRewardAnim.SetTrigger("get");
        //}

        void infoBtnClick()
        {
            infoBtnClickSub.OnNext(this);
        }
    }
}
