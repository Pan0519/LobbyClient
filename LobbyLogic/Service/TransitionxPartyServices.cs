
namespace Service
{
    class TransitionxPartyServices : TransitionBaseServices
    {
        public static TransitionxPartyServices instance = new TransitionxPartyServices();

        public override string transitionPath => "prefab/transition/transition_xparty";

        public override int closeAnimFrame => 50;
    }
}
