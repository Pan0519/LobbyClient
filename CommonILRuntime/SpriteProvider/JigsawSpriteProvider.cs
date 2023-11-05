using System.Collections.Generic;
using UnityEngine;

namespace Common.Jigsaw
{
    public static class JigsawSpriteProvider
    {
        static Dictionary<int, Season> seasons = new Dictionary<int, Season>();

        public static Sprite getAlbumSprite(int seasonId, int albumId, int pos)
        {
            return getSeason(seasonId).getAlbum(albumId).getPieceSprite(pos);
        }

        static Season getSeason(int seasonId)
        {
            Season outSeason = null;
            if (!seasons.TryGetValue(seasonId, out outSeason))
            {
                var season = new Season(seasonId);
                seasons.Add(seasonId, season);
                outSeason = season;
            }
            return outSeason;
        }
    }

    class Season
    {
        int season;
        Dictionary<int, Album> albums;

        public Season(int season)
        {
            this.season = season;
            albums = new Dictionary<int, Album>();
        }

        public Album getAlbum(int albumId)
        {
            Album outAlbum = null;
            if (!albums.TryGetValue(albumId, out outAlbum))
            {
                var album = new Album(season, albumId);
                albums.Add(albumId, album);
                outAlbum = album;
            }
            return outAlbum;
        }
    }

    class Album
    {
        string seasonSerial;
        string albumSerial;
        Dictionary<string, Sprite> sprites = null;

        public Album(int season, int album)
        {
            seasonSerial = season.ToString("000");
            albumSerial = album.ToString("00");
        }

        public Sprite getPieceSprite(int pos)
        {
            var pieceSerial = (pos-1).ToString("00");
            var spriteName = $"piece_{seasonSerial}_{albumSerial}_{pieceSerial}";
            Sprite outSprite = null;
            getSprites().TryGetValue(spriteName, out outSprite);
            return outSprite;
        }

        Dictionary<string, Sprite> getSprites()
        {
            if (null == sprites)
            {
                var iconSprites = ResourceManager.instance.loadAllWithResOrder($"/prefab/lobby_puzzle/pic/" +
                    $"puzzle_piece_{seasonSerial}_{albumSerial}/" +
                    $"puzzle_piece_{seasonSerial}_{albumSerial}",AssetBundleData.getBundleName(BundleType.LobbyPuzzle));
                sprites = Services.UtilServices.spritesToDictionary(iconSprites);
            }
            return sprites;
        }
    }
}
