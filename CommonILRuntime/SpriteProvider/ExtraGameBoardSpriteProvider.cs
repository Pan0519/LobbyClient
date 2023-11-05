using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CommonILRuntime.SpriteProvider
{
    public class ExtraGameBoardSpriteProvider : SpriteProviderBase
    {
        private const string spriteNameFormat = "save_the_dog_{0}";
        private const string repeatWinNameFormat = "retryboard_{0}";
        private const string loseNameFormat = "failedboard_{0}";
        private const string firstWinNameFormat = "passboard_{0}";

        public override Sprite[] loadSpriteArray()
        {
            List<Sprite> sprites = new List<Sprite>();
            string language = ApplicationConfig.nowLanguage.ToString().ToLower();

            sprites.AddRange(ResourceManager.instance.loadAll($"localization/{language}/res_save_the_dog_c_localization"));
            return sprites.ToArray();
        }

        public string convertGameOverBoardName(string spriteName, bool isWin)
        {
            string boardNameFormat = isWin ? repeatWinNameFormat : loseNameFormat;
            string result = string.Format(boardNameFormat, spriteName);

            return string.Format(spriteNameFormat, result);
        }

        public string convertFirstWinBoardName(string spriteName)
        {
            string result = string.Format(firstWinNameFormat, spriteName);

            return string.Format(spriteNameFormat, result);
        }
    }
}
