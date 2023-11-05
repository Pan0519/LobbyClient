using CommonILRuntime.Module;
using Lobby.VIP.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.VIP
{
    public class VipProfitValuePresenter : NodePresenter
    {
        Text valueText;
        Image bgImg;

        public override void initUIs()
        {
            valueText = getTextData("valueText");
            bgImg = getImageData("bgImg");
        }

        public void setData(VipProfit data)
        {
            //valueText.text = data.value.ToString();
            var v = data.value / 100.0f;
            valueText.text = $"x{v.ToString()}";
        }

        public void setBgSprite(Sprite sprite)
        {
            bgImg.sprite = sprite;
        }
    }
}
