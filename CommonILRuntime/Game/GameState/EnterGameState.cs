using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Slot
{
    using LobbyLogic.Audio;
    using System.Threading;

    public class EnterGameState: SlotGameState
    {
        public EnterGameState(SlotGameBase currentGame) : base(currentGame)
        {
            Debug.LogWarning("<<< EnterGameState Game >>>");
        }

        public override void StateBegin()
        {

        }

        public override void StateUpdate()
        {

        }
        public override void StateEnd()
        {

        }
    }
}