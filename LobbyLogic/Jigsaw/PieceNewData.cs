using UnityEngine;
using LitJson;
using System.Collections.Generic;

namespace Lobby.Jigsaw
{
    public static class PieceNewData
    {
        public static void saveData(string pieceID)
        {
            string albumID = pieceID.Substring(0, 5);
            NewData newData;
            if (PlayerPrefs.HasKey(albumID))
            {
                string jsonFile = PlayerPrefs.GetString(albumID);
                newData = JsonMapper.ToObject<NewData>(jsonFile);
            }
            else
            {
                newData = new NewData();
            }

            if (newData.pieceDatas.Exists(id => id.Equals(pieceID)))
            {
                return;
            }
            newData.pieceDatas.Add(pieceID);
            string saveJson = JsonMapper.ToJson(newData);
            PlayerPrefs.SetString(albumID, saveJson);
        }
    }

    public class NewData
    {
        public List<string> pieceDatas;

        public NewData()
        {
            pieceDatas = new List<string>();
        }

        public bool pieceDataIsNew(string pieceID)
        {
            if (null == pieceDatas)
            {
                return false;
            }

            return pieceDatas.Exists(id => id.Equals(pieceID));
        }
    }
}
