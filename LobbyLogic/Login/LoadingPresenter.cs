using Debug = UnityLogUtility.Debug;
using UnityEngine.UI;
using UnityEngine;
using UniRx;
using System;
using CommonILRuntime.Module;

namespace LobbyLogic.Login
{
    class LoadingPresenter : ContainerPresenter
    {
        public override string objPath => "prefab/lobby_login/loading";
        public override UiLayer uiLayer { get { return UiLayer.System; } }
        #region UIs
        Text loadingNumText;
        Slider barSlider;
        Text loadingInfoTxt;
        #endregion
        public Subject<float> progressChangeValued { get; private set; } = new Subject<float>();

        public override void initUIs()
        {
            loadingNumText = getTextData("loading_num_txt");
            barSlider = getBindingData<Slider>("bar_slider");
            loadingInfoTxt = getTextData("loading_info");
        }

        public override void init()
        {
            barSlider.onValueChanged.AddListener(setLoadingNum);
        }

        public void resetLoadingProgress()
        {
            setLoadingProgress(0);
        }

        public void setLoadingInfo(string info)
        {
            loadingInfoTxt.text = info;
        }

        public void setLoadingProgress(float value = 0f)
        {
            barSlider.value = value;
            progressChangeValued.OnNext(value);
        }
        public void setLoadingNum(float value)
        {
            int numValue = (int)(value * 100);
            loadingNumText.text = $"{numValue}%";
        }

        //public void startRunLoading()
        //{
        //    fakeLoadingDispos = Observable.EveryUpdate().Subscribe(_ =>
        //    {
        //        fakeLoading();
        //    }).AddTo(uiGameObject);
        //}

        //float time = 0;
        //IDisposable fakeLoadingDispos;
        //void fakeLoading()
        //{
        //    time += Time.deltaTime;
        //    if (time >= 0.2f)
        //    {
        //        if (loadingProgress >= 0.8f)
        //        {
        //            stopLoadingProgress();
        //            return;
        //        }
        //        loadingProgress += UnityEngine.Random.Range(0, 0.3f);
        //        loadingProgress = Mathf.Min(loadingProgress, 1);
        //        setLoadingProgress(loadingProgress);
        //        time = 0;
        //    }
        //}

        //public void stopLoadingProgress()
        //{
        //    if (null == fakeLoadingDispos)
        //    {
        //        return;
        //    }

        //    fakeLoadingDispos.Dispose();
        //}
    }
}
