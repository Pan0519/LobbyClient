using Common.VIP;
using CommonILRuntime.Module;

namespace Lobby.VIP
{
    public class VipSubjectUnit : NodePresenter
    {
        protected VipBoardSpriteProvider spriteProvider;

        public void setSpriteProvider(VipBoardSpriteProvider p)
        {
            spriteProvider = p;
        }
    }
}
