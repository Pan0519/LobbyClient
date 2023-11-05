using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Slot
{
    using LobbyLogic.Audio;
    using System.Threading;

    class WaitGameState: ExtraGameState
    {
        public WaitGameState(SlotGameBase currentGame) : base(currentGame)
        {
            Debug.LogWarning("<<< WaitGameState Game >>>");
        }

        public override void StateBegin()
        {
            
        }
    }
}