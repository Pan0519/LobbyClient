using CommonILRuntime.Module;
using UnityEngine;
using UnityEngine.UI;
using Common.Jigsaw;

namespace CommonPresenter.PackItem
{
    public class PackItemNodePresenter : NodePresenter
    {
        Image packImg;
        Image starImg;
        Animator packAnim;

        public override void initUIs()
        {
            packImg = getImageData("reward_pack_img");
            starImg = getImageData("star_img");
            packAnim = getAnimatorData("pack_anim");
        }

        public void showPackImg(PuzzlePackID puzzleID)
        {
            packImg.sprite = JigsawPackSpriteProvider.getPackSprite(puzzleID);
            Sprite starSprite = JigsawPackSpriteProvider.getPackStarSprite(puzzleID);
            starImg.gameObject.setActiveWhenChange(null != starSprite);
            starImg.sprite = starSprite;
            open();
        }

        public void playShowAnim()
        {
            packAnim.SetTrigger("open");
        }
    }
}
