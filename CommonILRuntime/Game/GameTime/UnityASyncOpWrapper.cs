using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class UnityASyncOpWrapper : IYieldWrapper
{
    private UnityEngine.AsyncOperation m_UnityObject;
    public bool finished
    {
        get
        {
            return m_UnityObject.isDone;
        }
    }

    public UnityASyncOpWrapper(UnityEngine.AsyncOperation wraps)
    {
        m_UnityObject = wraps;
    }
}

