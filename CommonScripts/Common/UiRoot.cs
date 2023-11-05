using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;

public class UiRoot : MonoSingleton<UiRoot>
{
    Transform mUiRoot;
    RectTransform mUiRootRect;
    Transform mSystemUiRoot;
    Transform mGameMessageRoot;
    Transform mUiBarRoot;
    Transform mTopUIRoot;
    Transform mLockHeightRoot;

    Transform mPropUiRoot;
    RectTransform mPropUiRootRect;
    Transform mPropSystemUiRoot;
    Transform mPropGameMessageRoot;
    Transform mPropUiBarRoot;
    Transform mPropTopUIRoot;

    const string landUIRootPath = "UI";
    const string propUIRootPath = "UI_Prop";
    const string uiRootPath = "UiRoot/UIBound";
    const string barRootPath = "UiBarRoot/UIBound";
    const string gameMsgRootPath = "GameMessageRoot/UIBound";
    const string systemUIRootPath = "SystemUiRoot/SysUIBound";
    const string topUIRootPath = "TopUiRoot/TopUIBound";
    #region Land UI Roots
    public Transform uiRoot
    {
        get
        {
            if (null == mUiRoot)
            {
                mUiRoot = GameObject.Find($"{landUIRootPath}/{uiRootPath}").transform;
            }

            return mUiRoot;
        }
    }

    public Transform uiBarRoot
    {
        get
        {
            if (null == mUiBarRoot)
            {
                mUiBarRoot = GameObject.Find($"{landUIRootPath}/{barRootPath}").transform;
            }

            return mUiBarRoot;
        }
    }

    public RectTransform uiRootRect
    {
        get
        {
            if (null == mUiRootRect)
            {
                mUiRootRect = (RectTransform)uiRoot;
            }

            return mUiRootRect;
        }
    }

    public Transform systemUiRoot
    {
        get
        {
            if (null == mSystemUiRoot)
            {
                mSystemUiRoot = GameObject.Find($"{landUIRootPath}/{systemUIRootPath}").transform;
            }

            return mSystemUiRoot;
        }
    }

    public Transform gameMessageRoot
    {
        get
        {
            if (null == mGameMessageRoot)
            {
                mGameMessageRoot = GameObject.Find($"{landUIRootPath}/{gameMsgRootPath}").transform;
            }

            return mGameMessageRoot;
        }
    }

    public Transform lockHeightRoot
    {
        get
        {
            if (null == mLockHeightRoot)
            {
                mLockHeightRoot = GameObject.Find($"{landUIRootPath}/LockHeightRoot/UIBound").transform;
            }
            return mLockHeightRoot;
        }
    }

    public Transform topUIRoot
    {
        get
        {
            if (null == mTopUIRoot)
            {
                mTopUIRoot = GameObject.Find($"{landUIRootPath}/{topUIRootPath}").transform;
            }
            return mTopUIRoot;
        }
    }
    #endregion

    #region Prop UI Roots
    public Transform propUIRoot
    {
        get
        {
            if (null == mPropUiRoot)
            {
                mPropUiRoot = GameObject.Find($"{propUIRootPath}/{uiRootPath}").transform;
            }

            return mPropUiRoot;
        }
    }

    public Transform propUIBarRoot
    {
        get
        {
            if (null == mPropUiBarRoot)
            {
                mPropUiBarRoot = GameObject.Find($"{propUIRootPath}/{barRootPath}").transform;
            }
            return mPropUiBarRoot;
        }
    }

    public RectTransform propUIRootRect
    {
        get
        {
            if (null == mPropUiRootRect)
            {
                mPropUiRootRect = (RectTransform)propUIRoot;
            }

            return mPropUiRootRect;
        }
    }

    public Transform propSystemUiRoot
    {
        get
        {
            if (null == mPropSystemUiRoot)
            {
                mPropSystemUiRoot = GameObject.Find($"{propUIRootPath}/{systemUIRootPath}").transform;
            }

            return mPropSystemUiRoot;
        }
    }

    public Transform propGameMessageRoot
    {
        get
        {
            if (null == mPropGameMessageRoot)
            {
                mPropGameMessageRoot = GameObject.Find($"{propUIRootPath}/{gameMsgRootPath}").transform;
            }

            return mPropGameMessageRoot;
        }
    }
    public Transform propTopUIRoot
    {
        get
        {
            if (null == mPropTopUIRoot)
            {
                mPropTopUIRoot = GameObject.Find($"{propUIRootPath}/{topUIRootPath}").transform;
            }
            return mPropTopUIRoot;
        }
    }
    #endregion

    public Transform getNowScreenOrientationUIRoot()
    {
        switch (getNowScreenOrientation)
        {
            case ScreenOrientation.Portrait:
            case ScreenOrientation.PortraitUpsideDown:
                return propUIRoot;
            default:
                return uiRoot;
        }
    }

    public RectTransform getNowScreenOrientationUIRootRect()
    {
        switch (getNowScreenOrientation)
        {
            case ScreenOrientation.Portrait:
            case ScreenOrientation.PortraitUpsideDown:
                return propUIRootRect;
            default:
                return uiRootRect;
        }
    }

    public Transform getNowScreenOrientationBarRoot()
    {
        switch (getNowScreenOrientation)
        {
            case ScreenOrientation.Portrait:
            case ScreenOrientation.PortraitUpsideDown:
                return propUIBarRoot;
            default:
                return uiBarRoot;
        }
    }

    public Transform getNowScreenOrientationSystemRoot()
    {
        switch (getNowScreenOrientation)
        {
            case ScreenOrientation.Portrait:
            case ScreenOrientation.PortraitUpsideDown:
                return propSystemUiRoot;
            default:
                return systemUiRoot;
        }
    }

    public Transform getNowScreenOrientationGameMsgRoot()
    {
        switch (getNowScreenOrientation)
        {
            case ScreenOrientation.Portrait:
            case ScreenOrientation.PortraitUpsideDown:
                return propGameMessageRoot;
            default:
                return gameMessageRoot;
        }
    }

    public Transform getNowScreenOrientationTopUIRoot()
    {
        switch (getNowScreenOrientation)
        {
            case ScreenOrientation.Portrait:
            case ScreenOrientation.PortraitUpsideDown:
                return propTopUIRoot;
            default:
                return topUIRoot;
        }
    }
    public GameObject propUIObj { get; private set; }
    public GameObject landUIObj { get; private set; }
    UICanvasHelper[] landCanvasHelpers;
    UICanvasHelper[] propCanvasHelpers;
    SafeAreaHelper[] safeAreaHelpers;


    void init()
    {
        resetUIs();
        landCanvasHelpers = new UICanvasHelper[] { };
        propCanvasHelpers = new UICanvasHelper[] { };
        safeAreaHelpers = new SafeAreaHelper[] { };
        propUIObj = GameObject.Find(propUIRootPath);
        landUIObj = GameObject.Find(landUIRootPath);
        landCanvasHelpers = landUIObj.GetComponentsInChildren<UICanvasHelper>();
        landUIObj.setActiveWhenChange(true);
        if (null != propUIObj)
        {
            propCanvasHelpers = propUIObj.GetComponentsInChildren<UICanvasHelper>();
            safeAreaHelpers = propUIObj.GetComponentsInChildren<SafeAreaHelper>();
            propUIObj.setActiveWhenChange(true);
        }
    }

    void resetUIs()
    {
        mUiRoot = null;
        mUiRootRect = null;
        mSystemUiRoot = null;
        mGameMessageRoot = null;
        mUiBarRoot = null;
        mTopUIRoot = null;
        mLockHeightRoot = null;

        mPropUiRoot = null;
        mPropUiRootRect = null;
        mPropSystemUiRoot = null;
        mPropGameMessageRoot = null;
        mPropUiBarRoot = null;
    }

    public async Task changeToLandscape()
    {
        init();

        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.orientation = ScreenOrientation.AutoRotation;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = false;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;

        await setCanvasScaler();
    }

    public async Task changeToPortrait()
    {
        init();
        Screen.orientation = ScreenOrientation.Portrait;
        await setCanvasScaler();
    }
    async Task setCanvasScaler()
    {
        setCanvasHelpers(landCanvasHelpers, ScreenOrientation.LandscapeLeft);
        setCanvasHelpers(propCanvasHelpers, ScreenOrientation.Portrait);

        await Task.Delay(TimeSpan.FromSeconds(0.3f));
        for (int i = 0; i < safeAreaHelpers.Length; ++i)
        {
            safeAreaHelpers[i].ApplySafeArea();
        }

    }

    void setCanvasHelpers(UICanvasHelper[] helpers, ScreenOrientation orientation)
    {
        for (int i = 0; i < helpers.Length; ++i)
        {
            var helper = helpers[i];
            helper.setScreenOrientaion(orientation);
            helper.setCanvasScaler();
        }
    }

    ScreenOrientation getNowScreenOrientation
    {
        get
        {
            switch (ApplicationConfig.NowRuntimePlatform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.OSXEditor:
                    if (Screen.height > Screen.width)
                    {
                        return ScreenOrientation.Portrait;
                    }
                    else
                    {
                        return ScreenOrientation.LandscapeLeft;
                    }

                default:
                    return Screen.orientation;
            }
        }
    }
}
