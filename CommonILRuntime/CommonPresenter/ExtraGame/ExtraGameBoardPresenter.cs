using CommonILRuntime.Module;
using CommonILRuntime.SpriteProvider;
using System;
using UniRx;
using UnityEngine;

namespace CommonILRuntime.ExtraGame
{
    public class ExtraGameBoardPresenter : NodePresenter
    {
        protected Animator animator;

        protected ExtraGameBoardSpriteProvider spriteProvider
        {
            get
            {
                return (ExtraGameBoardSpriteProvider)CommonSpriteProvider.instance.getSpriteProvider<ExtraGameBoardSpriteProvider>(CommonSpriteType.ExtraGameBoard);
            }
        }

        public override void initUIs()
        {
            animator = getAnimatorData("board_ani");
        }

        protected void playOutAni(Action aniOverAction)
        {
            animator.SetTrigger("out");
            Observable.TimerFrame(55).Subscribe((_) =>
                {
                    aniOverAction?.Invoke();
                    clear();
            }).AddTo(this.uiGameObject);
        }
    }
}
