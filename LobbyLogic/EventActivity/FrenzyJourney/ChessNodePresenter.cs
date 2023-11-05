using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using CommonService;
using System;
using Services;
using Game.Common;
using CommonILRuntime.BindingModule;
using UniRx;
using Lobby.Audio;
using LobbyLogic.Audio;

namespace FrenzyJourney
{
    class ChessNodePresenter : NodePresenter
    {
        Image headImg;
        Animator moveAnim;
        Action moveEndCB;
        Action mapMoving;

        BezierPresenter bezierPresenter;

        float posXOffset;
        bool isLastMove;

        public override void initUIs()
        {
            headImg = getImageData("player_img");
            moveAnim = getAnimatorData("chess_anim");
        }
        public override void init()
        {
            moveAnim.ResetTrigger("jump_fall");
            headImg.sprite = DataStore.getInstance.playerInfo.headSprite;
            bezierPresenter = UiManager.bind<BezierPresenter>(uiGameObject);
            posXOffset = (uiRectTransform.rect.width / 2);
        }

        public void setParent(Transform groundGroup, ItemGroupPresenter groundItem)
        {
            moveAnim.enabled = false;
            uiRectTransform.SetParent(groundGroup);
            uiRectTransform.localScale = Vector3.one;
            var newPos = groundItem.uiRectTransform.localPosition;
            newPos.Set(groundItem.uiRectTransform.localPosition.x + posXOffset, groundItem.uiRectTransform.localPosition.y, 0);
            uiRectTransform.localPosition = newPos;
            directionType = FrenzyJourneyData.getInstance.getRangeDireType(groundItem.groupID);
        }

        public void setMoveEndCB(Action cb)
        {
            moveEndCB = cb;
        }

        ChangeDirectionType directionType;
        public void movePos(Transform groundGroup, ItemGroupPresenter groundItem, bool isLastMove, Action moveCB = null)
        {
            moveAnim.enabled = true;
            this.isLastMove = isLastMove;
            mapMoving = moveCB;
            directionType = FrenzyJourneyData.getInstance.getChangeDirectionType(groundItem.groupID, directionType);
            bezierPresenter.bezierPoints.Clear();
            uiGameObject.setActiveWhenChange(true);
            uiRectTransform.SetParent(groundGroup);
            Vector2 endPos = groundItem.uiTransform.position;
            endPos.Set(endPos.x + 0.6f, endPos.y + 0.1f);
            Vector2 startPos = uiTransform.position;
            bezierPresenter.bezierPoints.Add(startPos);
            switch (directionType)
            {
                case ChangeDirectionType.Forward:
                    bezierPresenter.bezierPoints.Add(BezierUtils.getNormalDirShiftPoint(startPos, endPos, shiftQuant: 2));
                    break;

                case ChangeDirectionType.Up:
                    var upMiddlePos = BezierUtils.getNormalDirShiftPoint(startPos, endPos, shiftQuant: 0);
                    upMiddlePos.Set(upMiddlePos.x, upMiddlePos.y + 1.5f);
                    bezierPresenter.bezierPoints.Add(upMiddlePos);
                    break;

                case ChangeDirectionType.Down:
                    var downMiddlePos = BezierUtils.getNormalDirShiftPoint(startPos, endPos, shiftQuant: 0);
                    downMiddlePos.Set(downMiddlePos.x, downMiddlePos.y + 1.5f);
                    bezierPresenter.bezierPoints.Add(downMiddlePos);
                    break;
            }
            bezierPresenter.bezierPoints.Add(endPos);
            playJumpAnim(startMovePos);
        }

        void startMovePos()
        {
            AudioManager.instance.playAudioOnce(AudioPathProvider.getAudioPath(ActivityFJAudio.Jump));
            bezierPresenter.moveBezierLine(0.15f, () =>
            {
                if (isLastMove)
                {
                    playJumpAnim(moveEnd);
                    return;
                }
                moveEnd();
            });
            TweenManager.tweenPlay(bezierPresenter.bezierTweenID);
        }

        void moveEnd()
        {
            if (null != mapMoving)
            {
                mapMoving();
                return;
            }
            if (!isLastMove)
            {
                return;
            }

            if (null != moveEndCB)
            {
                moveEndCB();
            }
        }

        void playJumpAnim(Action completeCB)
        {
            IDisposable animTrigger = null;
            moveAnim.SetTrigger("jump_fall");
            animTrigger = Observable.TimerFrame(20).Subscribe(_ =>
             {
                 if (null != completeCB)
                 {
                     completeCB();
                 }
                 animTrigger.Dispose();
             }).AddTo(uiGameObject);
        }

        public void tweenKill()
        {
            if (null != bezierPresenter)
            {
                TweenManager.tweenKill(bezierPresenter.bezierTweenID);
            }
        }
    }
}
