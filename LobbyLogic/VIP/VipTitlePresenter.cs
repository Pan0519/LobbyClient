using Common.VIP;
using CommonILRuntime.Module;
using Lobby.VIP.UI;
using UnityEngine.UI;

namespace Lobby.VIP
{
    /// <summary>
    /// Vip等級元件
    /// </summary>
    public class VipTitlePresenter : VipSubjectUnit
    {
        Image icon;
        Image titleImg;

        public override void initUIs()
        {
            icon = getImageData("icon");
            titleImg = getImageData("titleImg");
        }

        public void setData(VipTitle data)
        {
            icon.sprite = VipSpriteGetter.getIconSprite(data.level);
            titleImg.sprite = VipSpriteGetter.getNameSprite(data.level);
        }
    }
}
