using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;

namespace Lobby
{
    public static class BottomBarLvTipManager
    {
        static List<LvTipNodePresenter> tips = new List<LvTipNodePresenter>();

        public static void resetTips()
        {
            tips.Clear();
        }

        public static void addBottomBarLvTips(LvTipNodePresenter tipNode)
        {
            tips.Add(tipNode);
        }

        public static void closeTips()
        {
            for (int i = 0; i < tips.Count; ++i)
            {
                tips[i].closelvTip();
            }
        }
    }
}
