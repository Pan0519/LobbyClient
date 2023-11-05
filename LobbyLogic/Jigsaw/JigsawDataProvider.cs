using UniRx;
using LobbyLogic.NetWork.ResponseStruct;
using Network;
using Service;
using Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using CommonILRuntime.Outcome;

namespace Lobby.Jigsaw
{
    /// <summary>
    /// 提供給拼圖冊相關的UI資料介面，不涉及伺服器API串接。
    /// 依據伺服器API獲得的資料，作客製化邏輯層的處理後回傳。
    /// </summary>
    public static class JigsawDataHelper
    {
        //單本圖冊拼圖數量
        const int ALBUM_TOTAL_PIECE_COUNT = 14;

        static JigsawDataProvider dataProvider = new JigsawDataProvider();

        public static Subject<string> recycleTimeSub = new Subject<string>();
        public static void updateRecycleTime(string time)
        {
            recycleTimeSub.OnNext(time);
        }

        public static async Task<string> getJigsawRecycleTime()
        {
            var recycleData = await dataProvider.syncRecycleTime();
            return recycleData.availableAfter;
        }

        /// <summary>
        /// 回傳本季總獎勵
        /// </summary>
        /// <returns></returns>
        public static async Task<long> getCurrentSeasonTotalReward()
        {
            var seasonAbstract = await dataProvider.syncCurrentSeasonAbstract();
            return null == seasonAbstract ? 0 : seasonAbstract.completeReward;
        }

        public static async Task<List<JigsawAlbumProgress>> getCurrentSeasonAllAlbumProgress()
        {
            var seasonAbstract = await dataProvider.syncCurrentSeasonAbstract();    //取得當季所有相簿
            List<JigsawAlbumProgress> progressList = new List<JigsawAlbumProgress>();
            var progress = await dataProvider.getSeasonAllAlbumProgress();  //取得有蒐集到的相簿進度
            for (int i = 0; i < seasonAbstract.albums.Length; i++)
            {
                var album = seasonAbstract.albums[i];
                var numCollected = 0;
                for (int pIdx = 0; pIdx < progress.Count; pIdx++)
                {
                    if (progress[pIdx].albumId.Equals(album.id))
                    {
                        numCollected = progress[pIdx].numCollected;
                        break;
                    }
                }
                progressList.Add(new JigsawAlbumProgress(album.id, numCollected));
            }
            return progressList;
        }

        /// <summary>
        /// 回傳本季單圖冊獎勵
        /// </summary>
        /// <returns></returns>
        public static long getCurrentSeasonAlbumReward(string albumId)
        {
            long reward = 0;
            var currentSeasonAbstract = dataProvider.cachedCurrentSeasonAbstract;
            if (null != currentSeasonAbstract)
            {
                var albums = currentSeasonAbstract.albums;
                if (null != albums)
                {
                    for (int i = 0; i < albums.Length; i++)
                    {
                        var album = albums[i];
                        if (album.id.Equals(albumId))
                        {
                            reward = album.completeReward;
                        }
                    }
                }
            }
            return reward;
        }

        /// <summary>
        /// 回傳本季圖冊總目標張數(包含未開放)
        /// </summary>
        /// <returns></returns>
        public static int getCurrentSeasonTargetPieces()
        {
            var currentSeasonAbstract = dataProvider.cachedCurrentSeasonAbstract;
            if (null != currentSeasonAbstract)
            {
                return ALBUM_TOTAL_PIECE_COUNT * currentSeasonAbstract.albums.Length;
            }
            return 0;
        }

        /// <summary>
        /// 回傳當季所有圖冊詳情(包含未開放)
        /// </summary>
        /// <returns></returns>
        public static async Task<List<JigsawAlbumData>> getCurrentSeasonAllAlbumDetail()
        {
            return await dataProvider.getCurrentSeasonAllAlbumDetail();
        }

        /// <summary>
        /// 回傳當季所有已開放的圖冊詳情
        /// </summary>
        /// <returns></returns>
        public static async Task<List<JigsawAlbumData>> getInTimeAllAlbumDetail()
        {
            var albumDetails = await dataProvider.getCurrentSeasonAllAlbumDetail();
            var outList = albumDetails.FindAll(albumData =>
           {
               return albumData.startedAt <= UtilServices.nowTime;
           });
            return outList;
        }

        /// <summary>
        /// 處理圖冊蒐集完成獲獎流程
        /// </summary>
        /// <returns></returns>
        public static async Task<List<JigsawRewardKind>> peekRewards()
        {
            var rewardArray = await dataProvider.peekJigsawRewards();
            return convertToList(rewardArray); ;
        }

        public static async Task<List<CommonReward>> redeemReward(string id)
        {
            var rewardArray = await dataProvider.redeemReward(id);
            return convertToList(rewardArray);
        }

        public static async Task<List<AlbumVoucher>> getAvaliableVouchers()
        {
            var vouchers = await dataProvider.getAllVouchers();
            var voucherList = convertToList(vouchers);
            var rtnList = voucherList.FindAll(data =>
           {
               return data.expiry > UtilServices.nowTime;
           });

            rtnList.Sort((AlbumVoucher x, AlbumVoucher y) =>
           {
               if (x.expiry < y.expiry)
               {
                   return -1;
               }
               else if (x.expiry > y.expiry)
               {
                   return 1;
               }
               return 0;
           });
            return rtnList;
        }

        static List<T> convertToList<T>(T[] array)
        {
            var list = new List<T>();
            if (null != array)
            {
                list.AddRange(array);
            }
            return list;
        }
    }

    /// <summary>
    /// 同步Server端拼圖資料來源，與伺服器來回的邏輯實作
    /// </summary>
    class JigsawDataProvider
    {
        /// <summary>
        /// 各季資料，以便未來擴充
        /// </summary>
        Dictionary<string, JigsawSeasonAbstract> seasons = new Dictionary<string, JigsawSeasonAbstract>();

        public JigsawDataProvider()
        {
            seasons.Clear();
        }

        /// <summary>
        /// 回傳目前季度概要
        /// </summary>
        /// <returns></returns>
        public JigsawSeasonAbstract cachedCurrentSeasonAbstract { get; private set; } = null;

        public async Task<List<JigsawAlbumSummary>> getSeasonAllAlbumProgress(string seasonId = "current")
        {
            List<JigsawAlbumSummary> allAlbumProgress = new List<JigsawAlbumSummary>();
            var response = await AppManager.lobbyServer.getJigsawAllAlbumProgress(seasonId);
            if (Result.OK == response.result)
            {
                for (int i = 0; i < response.summary.Length; i++)
                {
                    allAlbumProgress.Add(response.summary[i]);
                }
            }
            return allAlbumProgress;
        }

        /// <summary>
        /// 回傳當季所有拼圖冊拼圖完整內容 (包含未開放)
        /// 先取得當季概要，再依當季概要依序取得各圖冊完整內容
        /// </summary>
        /// <returns></returns>
        public async Task<List<JigsawAlbumData>> getCurrentSeasonAllAlbumDetail()
        {
            List<JigsawAlbumData> albumDetails = new List<JigsawAlbumData>();
            var season = await syncCurrentSeasonAbstract();
            if (null != season)
            {
                for (int i = 0; i < season.albums.Length; i++)
                {
                    var album = season.albums[i];
                    var albumPieces = await syncAlbumPieces(album.id);
                    if (null != albumPieces)
                    {
                        var startTime = null == album.startedAt ? season.startedAt : album.startedAt;
                        var endTime = season.endedAt;
                        var albumData = new JigsawAlbumData(album.id, albumPieces, startTime, endTime);
                        albumDetails.Add(albumData);
                    }
                }
            }
            return albumDetails;
        }

        public async Task<JigsawRecycle> syncRecycleTime()
        {
            return await AppManager.lobbyServer.getJigsawRecycle();
        }

        /// <summary>
        /// 同步當季所有相簿概要
        /// 相簿ID，開放、結束時間(包含未開放)
        /// </summary>
        /// <returns></returns>
        public async Task<JigsawSeasonAbstract> syncCurrentSeasonAbstract()
        {
            var response = await AppManager.lobbyServer.getJigsawCurrentSeasonAbstract();
            JigsawSeasonAbstract season = null;
            if (Result.OK == response.result)
            {
                season = response.season;
                try
                {
                    seasons.Add(season.id, season);
                }
                catch (ArgumentNullException)
                {
                    Debug.LogWarning("api: syncCurrentSeasonAbstract failed, currentSeason.id is null");
                    season = null;
                }
                catch (ArgumentException)
                {
                    //已經存過當季資料，用新的覆蓋
                    seasons.Remove(season.id);
                    seasons.Add(season.id, season);
                }
                cachedCurrentSeasonAbstract = season;
            }
            else
            {
                Debug.LogWarning($"api: syncCurrentSeasonAbstract result: {response.result}");
            }
            cachedCurrentSeasonAbstract = season;
            return season;
        }

        /// <summary>
        /// 同步指定相簿所有拼圖內容
        /// </summary>
        /// <param name="albumId"></param>
        /// <returns></returns>
        async Task<List<JigsawPieceData>> syncAlbumPieces(string albumId)
        {
            List<JigsawPieceData> pieces = new List<JigsawPieceData>();
            var response = await AppManager.lobbyServer.getJigsawAlbumDetail(albumId);
            if (Result.OK == response.result)
            {
                var jigsaws = response.content;
                for (int i = 0; i < jigsaws.Length; i++)
                {
                    var jigsaw = jigsaws[i];
                    pieces.Add(new JigsawPieceData(jigsaw.id, jigsaw.count));    //轉換成Client自己的資料格式
                }

                //sort by pos
                try
                {
                    pieces.Sort((a, b) =>
                    {
                        return a.getImagePos() < b.getImagePos() ? -1 : 1;
                    });
                }
                catch (ArgumentNullException)
                {
                    Debug.Log("api: syncAlbumPieces, sort pieces comparison delegate is null");
                }
                catch (ArgumentException)
                {
                    Debug.Log("api: syncAlbumPieces, caused an error during the sort");
                }
            }

            return pieces;
        }

        public async Task<JigsawRewardKind[]> peekJigsawRewards()
        {
            var response = await AppManager.lobbyServer.getAlbmRewards();
            if (Result.OK == response.result)
            {
                return response.rewards;
            }
            return null;
        }

        public async Task<CommonReward[]> redeemReward(string id)
        {
            var response = await AppManager.lobbyServer.redeemJigsawAlbumReward(id);
            if (Result.OK == response.result)
            {
                return response.rewards;
            }
            return null;
        }

        public async Task<AlbumVoucher[]> getAllVouchers()
        {
            var response = await AppManager.lobbyServer.getAllAlbumVouchers();
            if (Result.OK == response.result)
            {
                return response.vouchers;
            }
            return null;

            //TestData
            /*
            AlbumVoucher[] vouchers = new AlbumVoucher[] { new AlbumVoucher() { id = "test", type = "rarity-2", expiry = DateTime.UtcNow.AddSeconds(10)} };
            return vouchers;
            */
        }
    }
}
