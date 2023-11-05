using UnityEngine;
using CommonService;
using Services;
using System;
using System.Threading.Tasks;
using UnityEngine.UI;
using CommonILRuntime.Module;
using UniRx;

namespace Lobby.LoadingUIModule
{
    public class GameLoadingPresenter : NodePresenter
    {
        Image bgImg;
        Image gameImg;
        Text loadingInfo;
        Button closeBtn;
        Text barNumText;
        Image barProgressImg;
        GameObject loadingObj;
     
        public Action closeBtnClick;
        IDisposable _disposable = null;
        public override void initUIs()
        {
            bgImg = getImageData("bg_img");
            gameImg = getImageData("game_img");
            loadingInfo = getTextData("loading_info");
            closeBtn = getBtnData("closeButton");
            barProgressImg = getImageData("bar_progress_img");
            barNumText = getTextData("loading_num_txt");
            loadingObj = getGameObjectData("loading_obj");
        }

        public override void init()
        {
            closeBtn.onClick.AddListener(closeClick);
            setCloseBtnActivte(false);
            loadingObj.setActiveWhenChange(false);
        }

        public async void openCloseBtn()
        {
            await Task.Delay(TimeSpan.FromSeconds(2.0f));
            setCloseBtnActivte(false);
        }

        public void setCloseBtnActivte(bool active)
        {
            closeBtn.gameObject.setActiveWhenChange(active);
        }

        public void setLoadingInfo(string info)
        {
            loadingInfo.text = info;
        }

        public void setLoadingGameImg(string gameID)
        {
            gameImg.gameObject.setActiveWhenChange(false);
            if (string.IsNullOrEmpty(gameID))
            {
                return;
            }
            var sprite = loadingGameSprite(gameID, ApplicationConfig.nowLanguage);
            if (null == sprite && ApplicationConfig.nowLanguage != ApplicationConfig.Language.EN)
            {
                sprite = loadingGameSprite(gameID, ApplicationConfig.Language.EN);
            }
            if (null == sprite)
            {
                Debug.LogError($"Loading {gameID} Sprite is null");
                return;
            }
            gameImg.sprite = sprite;
            gameImg.gameObject.setActiveWhenChange(true);
        }

        public void openGameImg()
        {
            gameImg.gameObject.setActiveWhenChange(true);
        }

        Sprite loadingGameSprite(string gameID, ApplicationConfig.Language loadLanguage)
        {
            string language = loadLanguage.ToString().ToLower();
            return ResourceManager.instance.load<Sprite>($"prefab/game_loading/{language}/game_loading_{gameID}_{language}");
        }

        public void setLoadingBGImg(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                bgImg.gameObject.setActiveWhenChange(false);
                return;
            }

            if (ResourceManager.UiLoadFrom.Resources == ResourceManager.instance.resourceLoadFrom)
            {
                spriteName = $"{spriteName}.jpg";
            }
            var sprite = ResourceManager.instance.load<Sprite>($"prefab/game_loading/bg_game_loading_{spriteName}");
            if (null == sprite)
            {
                Debug.LogError($"Loading {spriteName} Sprite is null");
                return;
            }
            bgImg.gameObject.setActiveWhenChange(true);
            bgImg.sprite = sprite;
        }

        public void resetBarValue()
        {
            runLoadingProgress(0);
            loadingObj.setActiveWhenChange(true);
        }

        public void runLoadingProgress(float runVale)
        {
            barProgressImg.fillAmount = runVale;
            barValueChanged();
            //barSlider.value = Math.Max(defaultValue, runVale);
        }

        void barValueChanged()
        {
            barNumText.text = $"{(int)(barProgressImg.fillAmount * 100)}%";
        }

        void closeClick()
        {
            if (null != closeBtnClick)
            {
                closeBtnClick();
            }
        }

        public void clearFakeLoadingDispose()
        {
            if (null != _disposable) _disposable.Dispose();
        }
        public void fakeDefaultLoading(float startValue, float endValue, float time, Action callback = null)
        {
            clearFakeLoadingDispose();
            float actTime = time;
            float startTime = Time.time;
            float nowTime = 0;
            float progress = 0f;
            float stepValue = endValue - startValue;
            _disposable = Observable.EveryUpdate().Subscribe(_ =>
            {
                nowTime = Time.time - startTime;

                if (nowTime >= actTime)
                {
                    _disposable.Dispose();
                    progress = endValue;
                    callback?.Invoke();
                    //Util.Log("fakeDefaultLoading is dispose...");
                }
                else
                {
                    progress = startValue + ((stepValue) * nowTime / actTime);
                    //Util.Log($"fakeDefaultLoading:{progress}");
                }

                runLoadingProgress(progress);
            });

        }

    }
}
