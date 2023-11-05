using System.Collections.Generic;
using System.IO;

namespace Lobby.Audio
{
    public static class AudioPathProvider
    {
        private static readonly IReadOnlyDictionary<LobbyMainAudio, string> lobbyMainAudio = new Dictionary<LobbyMainAudio, string>()
        {
            { LobbyMainAudio.Main_BGM       ,"lobby_bgm_loop" },
            { LobbyMainAudio.Opening        ,"login" }
        };

        private static readonly IReadOnlyDictionary<AlbumAudio, string> albumAudio = new Dictionary<AlbumAudio, string>()
        {
            {AlbumAudio.BGM          ,"bgm_loop" },
            {AlbumAudio.Spin         ,"spin" },
            {AlbumAudio.SpinEnd      ,"spin_end" },
            {AlbumAudio.WinCard      ,"wincard" },
            {AlbumAudio.WinCardOpen  ,"wincard_open" },
        };

        #region Activity
        private static readonly IReadOnlyDictionary<ActivityBlastAudio, string> activityBlastAudio = new Dictionary<ActivityBlastAudio, string>()
        {
            {ActivityBlastAudio.MainBgm ,"main_bgm_loop" },
            {ActivityBlastAudio.BigWin  ,"bigwin" },
            {ActivityBlastAudio.IconFly ,"iconfly_1" }, //ICON移動
            {ActivityBlastAudio.IconFlyIn ,"iconfly_2" }, //播放進入效果
            {ActivityBlastAudio.Open    ,"open" },
            {ActivityBlastAudio.SmallWin,"win" },
            {ActivityBlastAudio.PrizeUpIconFly ,"prizeup_iconfly" }, //金幣加倍ICON飛入
            {ActivityBlastAudio.PrizeUpIconIn    ,"prizeup_iconappear" },//播放撞擊特效
            {ActivityBlastAudio.PrizeRunCoin,"prizeup_count" },//獎金上升
        };
        private static readonly IReadOnlyDictionary<ActivityFJAudio, string> activityFjAudio = new Dictionary<ActivityFJAudio, string>()
        {
            {ActivityFJAudio.MainBgm ,"main_bgm_loop" },
            {ActivityFJAudio.DiceLoop  ,"dice_loop" },
            {ActivityFJAudio.DiceStop ,"dice_stop" },
            {ActivityFJAudio.Jump ,"jump" },
            {ActivityFJAudio.MonsterBgm,"monster_bgm_loop" },
            {ActivityFJAudio.MonsterFight1,"monster_fighting_1" },
            {ActivityFJAudio.MonsterFight2 ,"monster_fighting_2" },
            {ActivityFJAudio.MonstoreMove    ,"monster_move" },
        };
        public static readonly IReadOnlyDictionary<ActivityMFAudio, string> activityMFAudio = new Dictionary<ActivityMFAudio, string>()
        {
            {ActivityMFAudio.MainBgm,"main_bgm_loop"},
            {ActivityMFAudio.Find,"find"},
            {ActivityMFAudio.Next,"next"},
            {ActivityMFAudio.Open,"open"},
            {ActivityMFAudio.OpenBag,"openbag"},
            {ActivityMFAudio.Rock,"rock"},
            {ActivityMFAudio.Shine,"shine"}
        };

        #endregion

        private static readonly IReadOnlyDictionary<SaveTheDogMapAudio, string> saveTheDogAudio = new Dictionary<SaveTheDogMapAudio, string>()
        {
            {SaveTheDogMapAudio.MainBgm          ,"quest_doge_lobby_bgm" },
            {SaveTheDogMapAudio.Gift             ,"quest_doge_lobby_gift" },
            {SaveTheDogMapAudio.Treasure         ,"quest_doge_lobby_treasure" },
            {SaveTheDogMapAudio.MapLine          ,"quest_doge_level" },
        };

        public static string getAudioPath(SaveTheDogMapAudio audio)
        {
            return Path.Combine("SaveTheDog", $"sound@{saveTheDogAudio[audio]}");
        }

        public static string getAudioPath(LobbyMainAudio audio)
        {
            return $"sound@{lobbyMainAudio[audio]}";
        }
        public static string getAudioPath(LoginAudio audio)
        {
            return Path.Combine("StampReward", $"sound@Login_{audio.ToString().ToLower()}");
        }

        public static string getAudioPath(BonusAudio audio)
        {
            return Path.Combine("StayGame", $"sound@Bonus_{audio.ToString().ToLower()}");
        }

        public static string getAudioPath(AlbumAudio audio)
        {
            return Path.Combine("Album", $"sound@Album_{albumAudio[audio]}");
        }
        #region Activity
        public static string getAudioPath(ActivityBlastAudio audio)
        {
            return Path.Combine("ActivityEvent/BLAST", $"sound@BLAST_{activityBlastAudio[audio]}");
        }
        public static string getAudioPath(ActivityFJAudio audio)
        {
            return Path.Combine("ActivityEvent/FJ", $"sound@FJ_{activityFjAudio[audio]}");
        }
        public static string getAudioPath(ActivityMFAudio audio)
        {
            return Path.Combine("ActivityEvent/MF", $"sound@MF_{activityMFAudio[audio]}");
        }
        #endregion
    }
    public enum LobbyMainAudio
    {
        Main_BGM,
        Opening
    }
    public enum LoginAudio
    {
        Stamp,
    }

    public enum BonusAudio
    {
        Appear,
        Award,
        AwardFly,
        Fall,
        Open,
    }

    public enum AlbumAudio
    {
        BGM,
        Spin,
        SpinEnd,
        WinCard,
        WinCardOpen
    }
    #region Activity
    public enum ActivityBlastAudio
    {
        BigWin,
        IconFly,
        IconFlyIn,
        MainBgm,
        Open,
        SmallWin,
        PrizeUpIconFly,
        PrizeUpIconIn,
        PrizeRunCoin,
    }

    public enum ActivityFJAudio
    {
        MainBgm,
        DiceLoop,
        DiceStop,
        Jump,
        MonsterBgm,
        MonsterFight1,
        MonsterFight2,
        MonstoreMove,
    }
    public enum ActivityMFAudio
    {
        MainBgm,
        Find,
        Next,
        Open,
        OpenBag,
        Rock,
        Shine,
    }

    public enum SaveTheDogMapAudio
    {
        MainBgm,
        Gift,
        Treasure,
        MapLine,
    }
    #endregion
}
