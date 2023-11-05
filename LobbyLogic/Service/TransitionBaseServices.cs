using UnityEngine;
using UniRx;

namespace Service
{
    public class TransitionBaseServices
    {
        public virtual string transitionPath { get; } = string.Empty;

        public virtual int closeAnimFrame { get; }

        GameObject _transitionObj = null;
        GameObject transitionObj
        {
            get
            {
                if (null == _transitionObj)
                {
                    if (string.IsNullOrEmpty(transitionPath))
                    {
                        return null;
                    }
                    _transitionObj = GameObject.Instantiate(ResourceManager.instance.getGameObject(transitionPath));
                    DontDestroyRoot.instance.addChildToCanvas(_transitionObj.transform);
                    _transitionObj.transform.localScale = Vector3.one;
                    RectTransform transitionRect = _transitionObj.transform as RectTransform;
                    transitionRect.offsetMax = Vector2.zero;
                    transitionRect.offsetMin = Vector2.zero;
                    outAnim = _transitionObj.GetComponentInChildren<Animator>();
                }

                return _transitionObj;
            }
        }
        Animator outAnim;

        public void openTransitionPage()
        {
            transitionObj.setActiveWhenChange(true);
        }

        public void closeTransitionPage()
        {
            if (null == outAnim)
            {
                transitionObj.setActiveWhenChange(false);
                return;
            }
            outAnim.SetTrigger("out");
            Observable.TimerFrame(closeAnimFrame).Subscribe(_ =>
            {
                transitionObj.setActiveWhenChange(false);
            });
        }
    }
}
