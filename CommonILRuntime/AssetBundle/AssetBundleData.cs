using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//namespace CommonService
//{
public enum BundleType
{
    Common,
    Lobby,
    LobbyMail,
    LobbyPuzzle,
    FarmBlast,
    FrenzyJourney,
    MagicForest,
    DailyMission,
    DcPet,
    Crown,
    LoginReward,
    StayMinigame,
    Vip,
    PublicityFarmBlast,
    PublicityFrenzyJourney,
    PublicityMagicForest,
    PublicitySaveTheDog,
    PublicityCasinoCrush,
    SaveTheDog,
    SaveDogGame
}
public static class AssetBundleData
{
    /*
    public enum BundleType
    {
        Common,
        Lobby,
        LobbyMail,
        LobbyPuzzle,
        FarmBlast,
        FrenzyJourney,
        MagicForest,
        DailyMission,
        DcPet,
        Crown,
        LoginReward,
        StayMinigame,
        Vip,
        PublicityFarmBlast,
        PublicityFrenzyJourney,
        PublicityMagicForest,
        PublicitySaveTheDog,
        PublicityCasinoCrush,
        SaveTheDog
    }*/
    static readonly Dictionary<BundleType, string> bundlesName = new Dictionary<BundleType, string>
    {
        { BundleType.Common,"common"},
        { BundleType.Lobby,"lobby"},
        { BundleType.LobbyMail,"lobby_mail"},
        { BundleType.LobbyPuzzle,"lobby_puzzle"},
        { BundleType.FarmBlast,"lobby_farm_blast"},
        { BundleType.FrenzyJourney,"lobby_frenzy_journey"},
        { BundleType.MagicForest,"lobby_magic_forest"},
        { BundleType.DailyMission,"lobby_daily_mission"},
        { BundleType.DcPet,"lobby_dc_pet"},
        { BundleType.Crown,"lobby_crown"},
        { BundleType.LoginReward,"lobby_login_reward"},
        { BundleType.StayMinigame,"lobby_stay_minigame"},
        { BundleType.Vip,"lobby_vip"},
        { BundleType.PublicityFarmBlast,"lobby_publicity_farm_blast"},
        { BundleType.PublicityFrenzyJourney,"lobby_publicity_frenzy_journey"},
        { BundleType.PublicityMagicForest,"lobby_publicity_magic_forest"},
        { BundleType.PublicitySaveTheDog,"lobby_publicity_save_the_dog"},
        { BundleType.PublicityCasinoCrush,"lobby_publicity_casino_crush"},
        { BundleType.SaveTheDog,"lobby_save_the_dog"},
        { BundleType.SaveDogGame,"savedog"},
    };

    public static string getBundleName(BundleType type)
    {
        if (!bundlesName.ContainsKey(type)) return "";
        return bundlesName[type];
    }
}
//}
