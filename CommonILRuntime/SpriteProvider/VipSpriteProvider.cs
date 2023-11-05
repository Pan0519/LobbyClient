using Services;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common.VIP
{
    public enum VipProfitDef
    {
        COIN_DEAL = 0,
        VIP_POINTS = 1,
        SILVER_BOX = 2,
        GOLDEN_BOX = 3,
        STORE_BONUS = 4,
        CARD_CRUSH_BOUNS,
    }

    public static class VipSpriteGetter
    {
        private static VipSpriteProvider provider = null;

        static VipSpriteProvider getProvider()
        {
            if (null == provider)
            {
                provider = new VipSpriteProvider();
            }
            return provider;
        }

        public static void clear()
        {
            if (null == provider)
            {
                return;
            }
            provider.clear();
            provider = null;
        }

        public static Sprite getIconSprite(int vipLevel)
        {
            return getProvider().getIconSprite(vipLevel);
        }

        public static Sprite getNameSprite(int vipLevel)
        {
            return getProvider().getNameSprite(vipLevel);
        }
    }

    public class VipSpriteProvider
    {
        Dictionary<string, Sprite> vipIconSprites;
        Dictionary<string, Sprite> vipNameSprites;

        public VipSpriteProvider()
        {
            //取得VIP icon圖集
            var iconSprites = ResourceManager.instance.loadAll("/texture/res_vip_rank_pic/res_vip_rank_pic");
            vipIconSprites = UtilServices.spritesToDictionary(iconSprites);

            //取得VIP 等級文字圖集
            string path = $"/localization/{ApplicationConfig.nowLanguage.ToString().ToLower()}/res_vip_rank_localization";
            var nameSprites = ResourceManager.instance.loadAll(path);
            vipNameSprites = UtilServices.spritesToDictionary(nameSprites);
        }

        public void clear()
        {
            vipIconSprites.Clear();
            vipNameSprites.Clear();
        }

        /// <summary>
        /// 命名規則: diamond_{0~N}
        /// </summary>
        /// <returns></returns>
        string getIconName(int id)
        {
            return $"diamond_{id}";
        }

        /// <summary>
        /// 命名規則: vip_{0~N}
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        string getTitleIconName(int id)
        {
            return $"vip_{id}";
        }

        int vipLevelToIconID(int vipLevel)
        {
            return Math.Max(vipLevel - 1, 0);   //伺服器從1開始，美術圖從0開始
        }

        public Sprite getIconSprite(int vipLevel)
        {
            Sprite s = null;
            var iconID = vipLevelToIconID(vipLevel);
            vipIconSprites.TryGetValue(getIconName(iconID), out s);
            return s;
        }

        public Sprite getNameSprite(int vipLevel)
        {
            Sprite s = null;
            var iconID = vipLevelToIconID(vipLevel);
            vipNameSprites.TryGetValue(getTitleIconName(iconID), out s);
            return s;
        }
    }

    public class VipBoardSpriteProvider
    {
        Dictionary<string, Sprite> profitNameSprites;
        Dictionary<string, Sprite> profitBgSprites;

        public VipBoardSpriteProvider()
        {
            //優惠項美術字
            var profitSprites = ResourceManager.instance.loadAll($"/localization/{ApplicationConfig.nowLanguage.ToString().ToLower()}/res_vip_ui_localization");
            profitNameSprites = UtilServices.spritesToDictionary(profitSprites);
            
            //優惠內容底圖
            var bgSprites = ResourceManager.instance.loadAllWithResOrder("/prefab/vip_info/res_vip/vip_list_red",AssetBundleData.getBundleName(BundleType.Vip));
            profitBgSprites = UtilServices.spritesToDictionary(bgSprites);
        }

        public Sprite getProfitSprite(VipProfitDef id)
        {
            Sprite s = null;
            string name = "";
            switch (id)
            {
                case VipProfitDef.COIN_DEAL:
                    name = "tex_coin_deal";
                    break;
                case VipProfitDef.VIP_POINTS:
                    name = "tex_vip";
                    break;
                case VipProfitDef.SILVER_BOX:
                    name = "tex_silver_box";
                    break;
                case VipProfitDef.GOLDEN_BOX:
                    name = "tex_golden_box";
                    break;
                case VipProfitDef.STORE_BONUS:
                    name = "tex_store_gift";
                    break;
            }
            profitNameSprites.TryGetValue(name, out s);
            return s;
        }

        public Sprite getPofitBgSprite(string name)
        {
            Sprite s = null;
            profitBgSprites.TryGetValue(name, out s);
            return s;
        }
    }
}
