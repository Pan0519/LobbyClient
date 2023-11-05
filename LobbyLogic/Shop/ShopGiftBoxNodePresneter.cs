using CommonILRuntime.BindingModule;
using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;

namespace Shop
{
    class ShopGiftBoxNodePresneter : NodePresenter
    {
        #region UIs
        Image boxImg;
        Text numTxt;
        GameObject rootObj;
        Animator gift_anim;
        #endregion

        public GiftInfoData giftInfo { get; private set; }

        public override void initUIs()
        {
            boxImg = getImageData("gift_img");
            numTxt = getTextData("box_num");
            rootObj = getGameObjectData("box_root");
            gift_anim = getAnimatorData("gift_anim");
        }

        public override async void open()
        {
            base.open();
        }

        public void showGiftBox(GiftInfoData infoData)
        {
            giftInfo = infoData;
            boxImg.sprite = ShopDataStore.getGiftStateSprite(infoData.giftState, isChoose: false);
            numTxt.text = $"{infoData.num}B";
            open();
        }

        public void changeIsChooseImg(bool isChoose)
        {
            boxImg.sprite = ShopDataStore.getGiftStateSprite(giftInfo.giftState, isChoose);
            //Debug.Log($"changeIsChooseImg {isChoose},Sprite {boxImg.sprite.name}");
        }

        public void giftPlayAnim()
        {
            gift_anim.SetTrigger("box_plus");
        }
    }
}
