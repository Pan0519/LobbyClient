using UnityEngine;


public class DefaultMsgBox
{
    public static DefaultMsgBox Instance { get { return _instance; } }

    static DefaultMsgBox _instance = new DefaultMsgBox();

    GameObject _msgBox = null;
    GameObject msgBox
    {
        get
        {
            if (null == _msgBox)
            {
                var boxTemp = Resources.Load("LobbyArt/default_page") as GameObject;
                _msgBox = GameObject.Instantiate(boxTemp);
                DontDestroyRoot.instance.addChildToCanvas(_msgBox.transform);
                _msgBox.transform.localScale = Vector3.one;
                var msgBoxRect = _msgBox.GetComponent<RectTransform>();
                msgBoxRect.offsetMax = Vector3.zero;
                msgBoxRect.offsetMin = Vector3.zero;
            }
            return _msgBox;
        }
    }
    CommonMsgBox commonMsgBox = null;

    public CommonMsgBox getMsgBox()
    {
        if (null == commonMsgBox)
        {
            commonMsgBox = msgBox.GetComponent<CommonMsgBox>();
        }
        return commonMsgBox;
    }
}
