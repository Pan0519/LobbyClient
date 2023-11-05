using UnityEngine;
using System;
using System.Threading.Tasks;

namespace Services
{
    public class UIRootChangeScreenServices
    {
        static UIRootChangeScreenServices _instance = new UIRootChangeScreenServices();
        public static UIRootChangeScreenServices Instance { get { return _instance; } }
        
        public async Task changeToLandscape()
        {
            await UiRoot.instance.changeToLandscape();
        }

        public async Task changeToPortrait()
        {
            await UiRoot.instance.changeToPortrait();
        }

        public void changeCameraDepth()
        {
            float landDepth = -1;
            float propDepth = -1;
            switch (UtilServices.getNowScreenOrientation)
            {
                case ScreenOrientation.Portrait:
                case ScreenOrientation.PortraitUpsideDown:
                    propDepth = 1;
                    break;

                default:
                    landDepth = 1;
                    break;
            }
            UiRoot.instance.landUIObj.GetComponentInChildren<Camera>().depth = landDepth;
            UiRoot.instance.propUIObj.GetComponentInChildren<Camera>().depth = propDepth;
        }

        public async Task justChangeScreenToLand()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            //Screen.orientation = ScreenOrientation.AutoRotation;
            //Screen.autorotateToLandscapeLeft = true;
            //Screen.autorotateToLandscapeRight = false;
            //Screen.autorotateToPortrait = false;
            //Screen.autorotateToPortraitUpsideDown = false;
            await Task.Delay(TimeSpan.FromSeconds(0.3f));
            UiRoot.instance.landUIObj.setActiveWhenChange(true);
            setPopUIActive(false);
            await Task.Delay(TimeSpan.FromSeconds(0.1f));
        }

        public async Task justChangeScreenToProp()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            await Task.Delay(TimeSpan.FromSeconds(0.3f));
            setPopUIActive(true);
            UiRoot.instance.landUIObj.setActiveWhenChange(false);
        }

        public void setOrientationObjActive()
        {
            switch (UtilServices.getNowScreenOrientation)
            {
                case ScreenOrientation.Portrait:
                case ScreenOrientation.PortraitUpsideDown:
                    UiRoot.instance.landUIObj.setActiveWhenChange(false);
                    break;

                default:
                    setPopUIActive(false);
                    break;
            }
        }

        void setPopUIActive(bool active)
        {
            if (null != UiRoot.instance.propUIObj)
            {
                UiRoot.instance.propUIObj.setActiveWhenChange(active);
            }
        }
    }
}
