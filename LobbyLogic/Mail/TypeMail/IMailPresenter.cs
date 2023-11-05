using System;
using UnityEngine;

namespace Lobby.Mail
{
    public interface IMailPresenter
    {
        void setData(IMessage data);
        void setReadedListener(Action listener);
        GameObject getObj();
        MailType mailType { get; }
    }
}
