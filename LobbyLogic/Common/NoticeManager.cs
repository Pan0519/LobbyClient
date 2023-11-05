using CommonPresenter;
using CommonService;
using EventActivity;
using Lobby.Jigsaw;
using LobbyLogic.NetWork.ResponseStruct;
using Mission;
using Service;
using Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniRx;
using Debug = UnityLogUtility.Debug;

namespace Lobby.Common
{
    public class NoticeManager
    {
        public static NoticeManager instance { get { return _instance; } }
        private static NoticeManager _instance = new NoticeManager();

        //駐留
        public int stayGameNoticeAmount = 0;
        public Subject<int> stayGameNoticeEvent = new Subject<int>();
        //救狗
        public int dogEventAmount { get; private set; }
        public Subject<int> dogeNoticeEvent = new Subject<int>();
        //信箱
        public Subject<int> mailNoticeEvent = new Subject<int>();
        //拼圖冊
        private const int spinWheelMinAmount = 15;
        private List<JigsawAlbumData> puzzleData = new List<JigsawAlbumData>();
        private bool isTimesUp = false;
        private int puzzleStarAmount = 0;
        public Subject<int> puzzleNoticeEvent = new Subject<int>();
        //每日任務
        public int dailyNoticeAmount = 0;
        public Subject<int> dailyNoticeEvent = new Subject<int>();
        //活動
        public Subject<int> activityNoticeEvent = new Subject<int>();
        //事件清單
        private List<IDisposable> eventList = new List<IDisposable>();

        private void clearEvent()
        {
            UtilServices.disposeSubscribes(eventList);
        }

        public void init()
        {
            clearEvent();
            //Debug.Log("Notice Manager On !!!");
            eventList.Add(JigsawReward.isJigsawShowFinish.Subscribe(getPuzzleStarAmount));
            eventList.Add(MiniGameConfig.instance.stayGameNoticeEvent.Subscribe(getStayGameNoticeAmount));
            eventList.Add(FromGameMsgService.getInstance.props.Subscribe(updateActivityPropAmount));
            eventList.Add(ActivityDataStore.activityCloseSub.Subscribe(getActivityPropNoticeAmount));
            getPuzzleStarAmount(true);
            getActivityPropNoticeAmount();
            getDailyNoticeAmount();
        }

        private async void getDailyNoticeAmount()
        {
            if (DataStore.getInstance.playerInfo.level < 10)
            {
                return;
            }
            
            await MissionData.updateData();
            dailyNoticeAmount = 0;
            int normalNotice = MissionData.normalMissionData.isComplete && !MissionData.normalMissionData.isRedeem ? 1 : 0;
            int specialNotice = MissionData.specialMissionData.isComplete && !MissionData.specialMissionData.isRedeem ? 1 : 0;
            dailyNoticeAmount += normalNotice + specialNotice;
            dailyNoticeEvent.OnNext(dailyNoticeAmount);
        }

        public void setDogEvent(int count)
        {
            dogEventAmount = count;
            dogeNoticeEvent.OnNext(count);
        }

        private void getStayGameNoticeAmount(List<StayGameType> stayGameList)
        {
            stayGameNoticeAmount = 0;
            for (var i = 0; i < stayGameList.Count; i++)
            {
                if (stayGameList[i] != StayGameType.none)
                {
                    stayGameNoticeAmount++;
                }
            }
            stayGameNoticeEvent.OnNext(stayGameNoticeAmount);
            //Debug.Log($"stayGameNoticeAmount : {stayGameNoticeAmount} !!!");
        }

        public async void getPuzzleStarAmount(bool isInit)
        {
            puzzleData = await JigsawDataHelper.getInTimeAllAlbumDetail();
            var PIECE_AVALIABLE_BASE_COUNT = 2;
            var content = puzzleData;
            List<JigsawPieceData> pieces = new List<JigsawPieceData>();
            for (int i = 0; i < content.Count; i++)
            {
                pieces.AddRange(content[i].pieces);
            }

            //拆選出符合規格條件者(擁有數量大於等於N張)
            var filteredPieces = pieces.FindAll(pieceData =>
            {
                return pieceData.getCount() >= PIECE_AVALIABLE_BASE_COUNT;
            });

            //setCount調整資料為可選擇最大數量
            puzzleStarAmount = 0;
            for (int i = 0; i < filteredPieces.Count; i++)
            {
                var filteredPiece = filteredPieces[i];
                filteredPiece.setCount(filteredPiece.getCount() - (PIECE_AVALIABLE_BASE_COUNT - 1));
                puzzleStarAmount += filteredPiece.getRareLevel() * filteredPiece.getStarLevel() * filteredPiece.getCount();
            }

            await checkPuzzleRecyleTime();
            int noticeAmount = puzzleStarAmount > spinWheelMinAmount && isTimesUp ? 1 : 0;
            puzzleNoticeEvent.OnNext(noticeAmount);
            //Debug.Log($"puzzleStarAmount : {puzzleStarAmount} !!!");
        }

        private async Task checkPuzzleRecyleTime()
        {
            string recycleTimeStr = await JigsawDataHelper.getJigsawRecycleTime();
            DateTime recycleTime = UtilServices.strConvertToDateTime(recycleTimeStr, DateTime.MinValue);
            CompareTimeResult compareResult = UtilServices.compareTimeWithNow(recycleTime);
            isTimesUp = compareResult == CompareTimeResult.Earlier;
        }

        public async void getActivityPropNoticeAmount(bool isGet = false)
        {
            var propResponse = await AppManager.lobbyServer.getActivityProp();
            activityNoticeEvent.OnNext(propResponse.prop.amount);
            //Debug.Log($"Activity Prop Amount : {propResponse.prop.amount} !!!");
        }

        private void updateActivityPropAmount(Props props)
        {
            if (null == props.outcome)
            {
                return;
            }
            Dictionary<string, object> bagDict;
            if (props.outcome.TryGetValue("bag", out bagDict))
            {
                int amount = (int)bagDict["amount"];
                activityNoticeEvent.OnNext(amount);
            }
        }
    }
}
