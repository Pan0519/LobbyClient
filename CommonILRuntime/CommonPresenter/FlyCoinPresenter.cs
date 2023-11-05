using CommonILRuntime.Module;
using UnityEngine.UI;
using UnityEngine;
using CommonILRuntime.BindingModule;
using Game.Common;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Threading;
using Services;
using CommonILRuntime.Services;
using CommonService;
using LobbyLogic.Audio;

namespace CommonILRuntime.CommonPresenter
{
    public class FlyCoinPresenter : ContainerPresenter, ILongValueTweenerHandler
    {
        public override string objPath { get { return "Prefab/coin_fly"; } }
        public override UiLayer uiLayer { get { return UiLayer.TopRoot; } }

        const int COINS_COUNT = 10;
        const float COIN_INTERVAL_SECONDS = 0.1f;
        const float REVERT_DELAY = 0.5f;

        const string FLY_EFFECT_PATH = "Prefab/coin_fly_effect";
        const string FLY_COIN_PATH = "Prefab/coin_fly_particle";

        //UI Bindings
        Image bgImg;
        RectTransform coinRoot;
        RectTransform coinTargetDummyLand;
        RectTransform playerMoneyRootLand;
        RectTransform coinTargetDummyPortrait;
        RectTransform playerMoneyRootPortrait;

        //fly use
        CancellationTokenSource cts = null;
        List<GameObject> startEndEffectList = new List<GameObject>();

        RectTransform coinTargetDummy;
        RectTransform playerMoneyRoot;

        bool isPlaySound = true;
        GameOrientation nowGameOrientation;
        float flyCoinScale;

        List<PoolObject> flyCoinObj = new List<PoolObject>();
        public override void initUIs()
        {
            bgImg = getImageData("bgImage");
            coinRoot = getRectData("coinRoot");
            coinTargetDummyLand = getRectData("coinTargetDummy");
            playerMoneyRootLand = getRectData("playerMoneyRoot");
            coinTargetDummyPortrait = getRectData("coinTargetDummy_portrait");
            playerMoneyRootPortrait = getRectData("playerMoneyRoot_portrait");
        }

        public override void init()
        {
            base.init();
        }

        public override async void open()
        {
            nowGameOrientation = await DataStore.getInstance.dataInfo.getNowGameOrientation();
            switch (nowGameOrientation)
            {
                case GameOrientation.Landscape:
                    playerMoneyRoot = playerMoneyRootLand;
                    coinTargetDummy = coinTargetDummyLand;
                    flyCoinScale = 1.0f;
                    break;

                case GameOrientation.Portrait:
                    playerMoneyRoot = playerMoneyRootPortrait;
                    coinTargetDummy = coinTargetDummyPortrait;
                    flyCoinScale = 0.8f;
                    break;
            }
            base.open();
            playerMoneyRoot.gameObject.setActiveWhenChange(true);
            DataStore.getInstance.playerMoneyPresenter.addTo(playerMoneyRoot);  //把玩家金幣UI物件黏到此presenter指定位置上
        }

        void free()
        {
            if (null != cts)
            {
                cts.Cancel();
            }

            for (int i = 0; i < startEndEffectList.Count; i++)
            {
                var obj = startEndEffectList[i];
                ResourceManager.instance.returnObjectToPool(obj);
            }
            startEndEffectList.Clear();
        }

        public override void close()
        {
            free();
            base.close();
        }

        public override void clear()
        {
            free();
            base.clear();
        }

        FlyCoinData flyCoinData = new FlyCoinData();

        public void setIsPlaySound(bool isPlay)
        {
            isPlaySound = isPlay;
        }

        public void frontSFly(RectTransform source, ulong sourceValue, ulong targetValue, float singleCoinFlySeconds, bool haveBackground, Action onComplete)
        {
            openAndSetData(source, sourceValue, targetValue, singleCoinFlySeconds, haveBackground, onComplete);

            //左右左
            Vector2 v1 = BezierUtils.getNormalDirShiftPoint(source.position, flyCoinData.middlePoint, 1.5f, true);
            Vector2 v2 = BezierUtils.getNormalDirShiftPoint(coinTargetDummy.position, flyCoinData.middlePoint, 1.5f, true);
            SFly(v1, v2);
        }

        public void obverseSFly(RectTransform source, ulong sourceValue, ulong targetValue, float singleCoinFlySeconds, bool haveBackground, Action onComplete)
        {
            openAndSetData(source, sourceValue, targetValue, singleCoinFlySeconds, haveBackground, onComplete);
            //右左右
            Vector2 v1 = BezierUtils.getNormalDirShiftPoint(flyCoinData.middlePoint, source.position, 1.5f, true);
            Vector2 v2 = BezierUtils.getNormalDirShiftPoint(flyCoinData.middlePoint, coinTargetDummy.position, 1.5f, true);
            SFly(v1, v2);
        }

        public void curveFly(RectTransform source, ulong sourceValue, ulong targetValue, float singleCoinFlySeconds, bool haveBackground, Action onComplete)
        {
            openAndSetData(source, sourceValue, targetValue, singleCoinFlySeconds, haveBackground, onComplete);
            Vector2 middleCurve = BezierUtils.getNormalDirShiftPoint(source.position, coinTargetDummy.position, 1f);
            List<Vector2> pathPos = new List<Vector2>() { flyCoinData.sourceRect.position, middleCurve, coinTargetDummy.position };
            coinFly(pathPos);
        }

        public void reverseCurveFly(RectTransform source, ulong sourceValue, ulong targetValue, float singleCoinFlySeconds, bool haveBackground, Action onComplete)
        {
            openAndSetData(source, sourceValue, targetValue, singleCoinFlySeconds, haveBackground, onComplete);
            Vector2 middleCurve = BezierUtils.getNormalDirShiftPoint(coinTargetDummy.position, source.position, 1f);
            List<Vector2> pathPos = new List<Vector2>() { flyCoinData.sourceRect.position, middleCurve, coinTargetDummy.position };
            coinFly(pathPos);
        }

        void openAndSetData(RectTransform source, ulong sourceValue, ulong targetValue, float singleCoinFlySeconds, bool haveBackground, Action onComplete)
        {
            open();
            bgImg.enabled = haveBackground;
            setFlyCoinData(source, sourceValue, targetValue, singleCoinFlySeconds, onComplete);
        }

        void SFly(Vector2 v1, Vector2 v2)
        {
            List<Vector2> pathPos = new List<Vector2>() { flyCoinData.sourceRect.position, v1, flyCoinData.middlePoint, v2, coinTargetDummy.position };
            coinFly(pathPos);
        }

        void setFlyCoinData(RectTransform source, ulong sourceValue, ulong targetValue, float singleCoinFlySeconds, Action onComplete)
        {
            flyCoinData.sourceRect = source;
            flyCoinData.sourceVal = sourceValue;
            flyCoinData.targetVal = targetValue;
            flyCoinData.flySeconds = singleCoinFlySeconds;
            flyCoinData.complete = onComplete;
            flyCoinData.middlePoint = (source.position + coinTargetDummy.position) / 2;
        }

        void coinFly(List<Vector2> pathPos)
        {
            cts = new CancellationTokenSource();
            flyCoins(flyCoinData.flySeconds, pathPos, flyCoinOnComplete, cts.Token);
            var tweenPointsDuration = (COINS_COUNT - 1) * COIN_INTERVAL_SECONDS;
            tweenPoints(flyCoinData.sourceVal, flyCoinData.targetVal, flyCoinData.flySeconds, tweenPointsDuration, flyCoinData.complete);
        }

        async void tweenPoints(ulong sourceValue, ulong targetValue, float delaySeconds, float totalSeconds, Action onComplete)
        {
            onValueChanged(sourceValue);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

            var delta = Math.Max(targetValue - sourceValue, 0);
            ulong frequency = (ulong)(delta / totalSeconds);
            var coinTweener = new LongValueTweener(this, frequency);
            coinTweener.onComplete = () =>
            {
                onComplete?.Invoke();
                delayRevert();
            };
            coinTweener.setRange(sourceValue, targetValue);
        }

        async void delayRevert()
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(REVERT_DELAY));
                DataStore.getInstance.playerMoneyPresenter.returnToLastParent();
                await Task.Delay(TimeSpan.FromSeconds(0.5f));
                clear();
                await Task.Delay(TimeSpan.FromSeconds(0.5f));
                CoinFlyManager.isCoinFlyFinish = false;
            }
            catch (Exception e)
            {
                Debug.Log($"exception {e.Message}");
            }
        }

        void flyCoinOnComplete()
        {
            AudioManager.instance.stopOnceAudio();
        }

        public void onValueChanged(ulong value)
        {
            DataStore.getInstance.playerMoneyPresenter.setMoney(value.ToString("N0"));
        }

        public GameObject getDisposableObj()
        {
            return uiGameObject;
        }

        void showStartEndEffect(Vector2 pos)
        {
            var startEffect = ResourceManager.instance.getObjectFromPool(FLY_EFFECT_PATH, coinRoot);
            //changeObjScale(startEffect);
            startEffect.cachedTransform.position = pos;
            startEffect.cachedGameObject.setActiveWhenChange(true);
            startEndEffectList.Add(startEffect.cachedGameObject);
        }

        void flySingleCoin(float duration, List<Vector2> pathPos, Action onComplete)
        {
            try
            {
                showStartEndEffect(pathPos[0]); //起點特效

                var obj = ResourceManager.instance.getObjectFromPool(FLY_COIN_PATH, coinRoot, COINS_COUNT + 1);
                //changeObjScale(obj);
                var flyObj = obj.cachedGameObject;
                flyCoinObj.Add(obj);
                var pathController = UiManager.bind<BezierPresenter>(flyObj);
                pathController.bezierPoints = pathPos;
                flyObj.setActiveWhenChange(true);

                pathController.moveBezierLine(duration, () =>
                {
                    if (null == flyObj)
                    {
                        return;
                    }
                    var particleSys = flyObj.GetComponentsInChildren<ParticleSystem>();
                    if (null != particleSys)
                    {
                        for (int i = 0; i < particleSys.Length; i++)
                        {
                            var p = particleSys[i];
                            p.Clear(true);
                        }
                    }

                    flyObj.setActiveWhenChange(false);
                    //ResourceManager.instance.returnObjectToPool(flyObj);
                    showStartEndEffect(pathPos[pathPos.Count - 1]);   //終點撞擊特效
                    onComplete?.Invoke();
                });
                TweenManager.tweenPlay(pathController.bezierTweenID);
            }
            catch (Exception e)
            {
                Debug.Log($"flySingleCoin Exception {e.Message}");
            }
        }

        //void changeObjScale(PoolObject poolObj)
        //{
        //    var changeScale = poolObj.cachedRectTransform.localScale;
        //    changeScale = changeScale * flyCoinScale;
        //    poolObj.cachedRectTransform.localScale = changeScale;
        //}

        async void flyCoins(float duration, List<Vector2> pathPos, Action onComplete, CancellationToken token)
        {
            if (isPlaySound)
            {
                AudioManager.instance.playAudioOnce(CommonAudioPathProvider.getAudioPath(BasicCommonSound.FlyCoin));
            }
            for (int i = 1; i <= COINS_COUNT; i++)
            {
                if (token.IsCancellationRequested)
                {
                    onComplete?.Invoke();
                    AudioManager.instance.stopOnceAudio();
                    break;
                }

                Action callback = null;
                if (COINS_COUNT == i)
                {
                    callback = onComplete;
                    callback += returnCoinFlyObj;
                }
                flySingleCoin(duration, pathPos, callback);
                await Task.Delay(TimeSpan.FromSeconds(COIN_INTERVAL_SECONDS));
            }
        }

        void returnCoinFlyObj()
        {
            for (int i = 0; i < flyCoinObj.Count; ++i)
            {
                ResourceManager.instance.returnObjectToPool(flyCoinObj[i].cachedGameObject);
            }

            flyCoinObj.Clear();
        }

        public override void destory()
        {
            cts.Cancel();
            base.destory();
        }
    }

    class FlyCoinData
    {
        public RectTransform sourceRect;
        public ulong sourceVal;
        public ulong targetVal;
        public float flySeconds;
        public Action complete;
        public Vector2 middlePoint;
    }
}
