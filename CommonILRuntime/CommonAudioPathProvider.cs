using System.Collections.Generic;

namespace CommonService
{
    public enum BasicCommonSound
    {
        InfoBtn,
        PlusbetBtn,
        MinusBetBtn,
        MaxbetBtn,
        SpinBtn,
        SwitchBtn,
        BuyBtn,
        FlyCoin,
        PopupEffect,
    }
    public enum MainGameCommonSound
    {
        BigWin,
        MegaWin,
        EpicWin,
        MassiveWin,
        UltimateWin,
        MainReelStop,
        LvUpSmall,
        LvUpBig,
        NicwWin,
        AmazingWin,
        IncredibleWin,
        LockBtn,
        UnlockBtn,
        CoinFall
    }

    public static class CommonAudioPathProvider
    {
        private static readonly IReadOnlyDictionary<BasicCommonSound, string> basicAudioPaths = new Dictionary<BasicCommonSound, string>()
        {
            {BasicCommonSound.InfoBtn,       "btn_info"     },
            {BasicCommonSound.PlusbetBtn,    "btn_plusbet"  },
            {BasicCommonSound.MinusBetBtn,   "btn_minusbet" },
            {BasicCommonSound.MaxbetBtn,     "btn_maxbet"   },
            {BasicCommonSound.SpinBtn,       "btn_spin"     },
            {BasicCommonSound.SwitchBtn,     "btn_switch"   },
            {BasicCommonSound.BuyBtn,        "buy"          },
            {BasicCommonSound.FlyCoin,       "flycoin"      },
            {BasicCommonSound.PopupEffect,   "popup"        },
        };
        private static readonly IReadOnlyDictionary<MainGameCommonSound, string> maingameAudioPaths = new Dictionary<MainGameCommonSound, string>()
        {
            {MainGameCommonSound.BigWin,          "bigwin"        },
            {MainGameCommonSound.MegaWin,         "megawin"       },
            {MainGameCommonSound.EpicWin,         "epicwin"       },
            {MainGameCommonSound.MassiveWin,      "massivewin"    },
            {MainGameCommonSound.UltimateWin,     "ultimatewin"   },
            {MainGameCommonSound.MainReelStop,    "main_reelstop" },
            {MainGameCommonSound.LvUpBig ,        "lvup_big"      },
            {MainGameCommonSound.LvUpSmall ,      "lvup_small"    },
            {MainGameCommonSound.NicwWin ,        "nice"          },
            {MainGameCommonSound.AmazingWin ,     "amazing"       },
            {MainGameCommonSound.IncredibleWin ,  "incredible"    },
            {MainGameCommonSound.LockBtn,         "btn_lock"      },
            {MainGameCommonSound.UnlockBtn,       "btn_unlock"    },
            {MainGameCommonSound.CoinFall,        "coinfall_s"    },
        };

        public static string getAudioPath(BasicCommonSound audio)
        {
            return $"Basic/sound@{basicAudioPaths[audio]}"; ;
        }
        public static string getAudioPath(MainGameCommonSound audio)
        {
            return $"MainGame/sound@{maingameAudioPaths[audio]}"; ;
        }

    }
}
