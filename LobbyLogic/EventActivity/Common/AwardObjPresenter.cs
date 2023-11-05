using CommonILRuntime.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Event.Common
{
    class TxtAmountAwardObjPresenter : NodePresenter
    {
        #region [BindingField]
        Text txt_Amount;
        #endregion

        public override void initUIs()
        {
            txt_Amount = getTextData("txt_Amount");
        }

        public TxtAmountAwardObjPresenter setAmount(string content)
        {
            txt_Amount.text = content;
            return this;
        }
    }

    class CustomTxtAmountAwardObjPresenter : NodePresenter
    {
        #region [BindingField]
        CustomTextSizeChange txt_Amount;
        #endregion

        public override void initUIs()
        {
            txt_Amount = getBindingData<CustomTextSizeChange>("txt_Amount");
        }

        public CustomTxtAmountAwardObjPresenter setAmount(string content)
        {

            txt_Amount.text = content;
            return this;
        }
    }

    class TicketAwardPresenter : CustomTxtAmountAwardObjPresenter
    {
        GameObject amountGroup;
        Animator ticketAnim;

        public override void initUIs()
        {
            base.initUIs();
            amountGroup = getGameObjectData("amount_group_obj");
            ticketAnim = getAnimatorData("ticket_anim");
        }

        public override void init()
        {
            ticketAnim.enabled = true;
            amountGroup.setActiveWhenChange(true);
        }
    }

    class SpriteAwardObjPresenter : NodePresenter
    {
        #region [BindingField]
        Image iconImg;
        #endregion

        public override void initUIs()
        {
            iconImg = getImageData("icon_img");
        }

        public SpriteAwardObjPresenter setSprite(Sprite sprite)
        {
            iconImg.sprite = sprite;
            return this;
        }
    }
}
