using CommonILRuntime.Outcome;
using System;
using Services;
using System.Collections.Generic;
using UniRx;
using Service;

namespace Lobby.Jigsaw
{
    public static class OpenPackWildProcess
    {
        static List<CommonReward> puzzlePack = new List<CommonReward>();
        static CommonReward puzzleVoucher;
        static Action finishCallback;

        static IDisposable finishSub;

        public static async void openPackWildFromID(string rewardPackID, Action callback = null)
        {
            var rewardPacket = await AppManager.lobbyServer.getRewardPacks(rewardPackID);
            openPackWild(rewardPacket.rewards, callback);
        }

        public static void openPackWild(CommonReward[] commonRewards, Action callback = null)
        {
            puzzlePack.Clear();
            puzzleVoucher = null;
            finishCallback = callback;
            for (int i = 0; i < commonRewards.Length; ++i)
            {
                var reward = commonRewards[i];
                if (reward.kind.Equals(UtilServices.outcomePuzzlePack))
                {
                    puzzlePack.Add(reward);
                    continue;
                }
                if (reward.kind.Equals(UtilServices.outcomePuzzleVoucher))
                {
                    puzzleVoucher = reward;
                }

            }
            if (puzzlePack.Count > 0)
            {
                JigsawPack.OpenPackRewards(puzzlePack, openPuzzleVoucher);
                return;
            }

            openPuzzleVoucher();
        }

        static void openPuzzleVoucher()
        {
            UtilServices.disposeSubscribes(finishSub);
            finishSub = JigsawReward.isJigsawShowFinish.Subscribe(_ =>
            {
                if (null != finishCallback)
                {
                    finishCallback();
                    finishCallback = null;
                }
                finishSub.Dispose();
            });
            if (null == puzzleVoucher)
            {
                openPuzzleReward();
                return;
            }

            WildPack.openWildPack(puzzleVoucher, openPuzzleReward);
        }

        static void openPuzzleReward()
        {
            JigsawReward.checkCollectionRewards();
        }
    }
}
