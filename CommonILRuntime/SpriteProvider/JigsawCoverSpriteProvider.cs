using Services;
using System.Collections.Generic;
using UnityEngine;

namespace Common.Jigsaw
{
    public static class JigsawCoverSpriteProvider
    {
        static Dictionary<string, Covers> seasons = new Dictionary<string, Covers>();

        public static Sprite getAlbumCover(string albumId)
        {
            var seasonId = albumId.Substring(0, 3);
            var albumSerial = albumId.Substring(3);
            return getSeasonCovers(seasonId).getCover(albumSerial);
        }

        static Covers getSeasonCovers(string seasonId)
        {
            Covers covers = null;
            if (!seasons.TryGetValue(seasonId, out covers))
            {
                var newSeasonCovers = new Covers(seasonId);
                seasons.Add(seasonId, newSeasonCovers);
                covers = newSeasonCovers;
            }
            return covers;
        }
    }

    class Covers
    {
        string seasonSerial;
        Dictionary<string, Sprite> coverSprites = null;

        public Covers(string seasonId)
        {
            seasonSerial = seasonId;
        }

        public Sprite getCover(string albumId)
        {
            string albumSerial = albumId;
            string spriteName = $"puzzle_cover_{seasonSerial}_{albumSerial}";
            Sprite outSprite = null;
            covers.TryGetValue(spriteName, out outSprite);
            return outSprite;
        }

        Dictionary<string, Sprite> covers
        {
            get
            {
                if (null == coverSprites)
                {
                    var sprites = ResourceManager.instance.loadAllWithResOrder($"/prefab/lobby_puzzle/pic/" +
                        $"puzzle_cover_{seasonSerial}/" +
                        $"puzzle_cover_{seasonSerial}", AssetBundleData.getBundleName(BundleType.LobbyPuzzle));
                    coverSprites = UtilServices.spritesToDictionary(sprites);
                }
                return coverSprites;
            }
        }
    }

    public static class JigsawLogoSpriteProvider
    {
        static Dictionary<string, Logos> seasons = new Dictionary<string, Logos>();

        public static Sprite getAlbumLogo(string albumId)
        {
            var seasonId = albumId.Substring(0, 3);
            var albumSerial = albumId.Substring(3);
            return getSeasonLogos(seasonId).getLogo(albumSerial);
        }

        static Logos getSeasonLogos(string seasonId)
        {
            Logos logos = null;
            if (!seasons.TryGetValue(seasonId, out logos))
            {
                var newSeasonLogos = new Logos(seasonId);
                seasons.Add(seasonId, newSeasonLogos);
                logos = newSeasonLogos;
            }
            return logos;
        }
    }

    class Logos
    {
        string seasonSerial;
        Dictionary<string, Sprite> logoSprites = null;

        public Logos(string seasonId)
        {
            seasonSerial = seasonId;
        }

        public Sprite getLogo(string albumId)
        {
            string albumSerial = albumId;
            string spriteName = $"puzzle_logo_{seasonSerial}_{albumSerial}";
            Sprite outSprite = null;
            logos.TryGetValue(spriteName, out outSprite);
            return outSprite;
        }

        Dictionary<string, Sprite> logos
        {
            get
            {
                if (null == logoSprites)
                {
                    var sprites = ResourceManager.instance.loadAllWithResOrder($"/prefab/lobby_puzzle/pic/" +
                        $"puzzle_logo_{seasonSerial}/" +
                        $"puzzle_logo_{seasonSerial}",AssetBundleData.getBundleName(BundleType.LobbyPuzzle));
                    logoSprites = UtilServices.spritesToDictionary(sprites);
                }
                return logoSprites;
            }
        }
    }

    public static class JigsawPackSpriteProvider
    {
        static Dictionary<string, Sprite> packsSprite
        {
            get
            {
                if (null == _packSprite)
                {
                    List<Sprite> sprites = new List<Sprite>();
                    sprites.AddRange(ResourceManager.instance.loadAllWithResOrder("prefab/lobby_puzzle/pic/puzzle_pack/puzzle_pack",AssetBundleData.getBundleName(BundleType.LobbyPuzzle)));
                    sprites.AddRange(ResourceManager.instance.loadAll("texture/board_common/pic/puzzle_pack_wild"));
                    _packSprite = UtilServices.spritesToDictionary(sprites.ToArray());
                }
                return _packSprite;
            }
        }

        static Dictionary<string, Sprite> _packSprite;

        static Sprite getPackSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                return null;
            }

            Sprite result = null;
            if (!packsSprite.TryGetValue(spriteName, out result))
            {
                Debug.LogError($"get {spriteName} packSpite is null");
            }

            return result;
        }

        public static OpenPackSprites getOpenPackSprites(PuzzlePackID packId)
        {
            string puzzlePackName = convertToSpriteStartName(packId);

            OpenPackSprites packSprites = new OpenPackSprites();
            packSprites.upSprite = getPackSprite($"pack_b_{puzzlePackName}");
            packSprites.downSprite = getPackSprite($"pack_a_{puzzlePackName}");
            return packSprites;
        }

        static List<string> packImgNames = null;
        static string[] packStarNames = new string[] { "OneStar", "TwoStar", "ThreeStar", "FourStar", "FiveStar" };

        static string convertToSpriteStartName(PuzzlePackID packID)
        {
            string packIDStr = packID.ToString();
            if (packIDStr.EndsWith("Wild"))
            {
                packIDStr = packIDStr.Replace("Wild", "").Trim();
            }
            else
            {
                int packStarID = getPackStarID(packID);
                if (packStarID > 0)
                {
                    string starName = packStarNames[packStarID - 1];
                    packIDStr = packIDStr.Replace(starName, "").Trim();
                }
            }

            if (null == packImgNames)
            {
                packImgNames = new List<string>() { "Green_Blue" };
                for (long i = (long)PuzzlePackID.Green; i <= (long)PuzzlePackID.Color; ++i)
                {
                    PuzzlePackID nameID = (PuzzlePackID)i;
                    packImgNames.Add(nameID.ToString());
                }
            }

            for (int i = 0; i < packImgNames.Count; ++i)
            {
                string packSpriteName = packImgNames[i];
                if (packIDStr.Equals(packSpriteName))
                {
                    return packSpriteName.ToLower();
                }
            }

            return string.Empty;
        }
        public static Sprite getPackSprite(PuzzlePackID packID)
        {
            string packIDStr = packID.ToString();
            string packSpriteName = convertToSpriteStartName(packID);

            if (string.IsNullOrEmpty(packSpriteName))
            {
                return null;
            }

            string spriteEndName = (packIDStr.EndsWith("Wild")) ? "wild" : "normal";
            return getPackSprite($"puzzle_pack_{packSpriteName}_{spriteEndName}");
        }

        public static Sprite getPackStarSprite(PuzzlePackID packID)
        {
            int starID = getPackStarID(packID);
            if (starID <= 0)
            {
                return null;
            }
            return getPackSprite($"pack_rare_{starID}");
        }

        public static int getPackStarID(PuzzlePackID packID)
        {
            string packIDStr = packID.ToString();
            for (int i = 0; i < packStarNames.Length; ++i)
            {
                string result = packStarNames[i];
                if (packIDStr.EndsWith(result))
                {
                    return i + 1;
                }
            }

            return 0;
        }
    }

    public class OpenPackSprites
    {
        public Sprite upSprite;
        public Sprite downSprite;
    }
    /// <summary>
    /// 拼圖卡包對應ServerID
    /// </summary>
    public enum PuzzlePackID
    {
        Green = 30101, //綠色隨機抽卡包
        Blue,          //藍色隨機抽卡包
        Gold,          //金色隨機抽卡包
        Color,         //隨機抽卡包
        GreenThreeStar,//綠色三星抽卡包
        GreenFourStar, //綠色四星抽卡包
        GreenFiveStar, //綠色五星抽卡包
        BlueThreeStar, //藍色三星抽卡包
        BlueFourStar,  //藍色四星抽卡包
        BlueFiveStar,  //藍色五星抽卡包
        GoldThreeStar, //金色三星抽卡包
        GoldFourStar,  //金色四星抽卡包
        GoldFiveStar,  //金色五星抽卡包
        Green_BlueTwoStar,   //二星綠藍抽卡包
        Green_BlueThreeStar, //三星綠藍抽卡包
        Green_BlueFourStar,  //四星綠藍抽卡包
        Green_BlueFiveStar,  //五星綠藍抽卡包
        GoldOneStar,   //金色一星抽卡包
        GoldTwoStar,   //金色二星抽卡包

        GreenWild = 101,     //綠色WILD包
        BlueWild,      //藍色WILD包
        GoldWild,      //金色WILD包
        ColorWild,      //WILD包
    }

    public static class PuzzleTypeConverter
    {
        public static string wildTypeToPuzzlePackID(string type)
        {
            PuzzlePackID id = PuzzlePackID.Green;  //防呆

            switch (type)
            {
                case "rarity-all":
                    {
                        id = PuzzlePackID.ColorWild;
                    }
                    break;
                case "rarity-2":
                    {
                        id = PuzzlePackID.GoldWild;
                    }
                    break;
                case "rarity-1":
                    {
                        id = PuzzlePackID.BlueWild;
                    }
                    break;
                case "rarity-0":
                    {
                        id = PuzzlePackID.GreenWild;
                    }
                    break;
            }
            int iId = (int)id;
            return iId.ToString();
        }
    }
}
