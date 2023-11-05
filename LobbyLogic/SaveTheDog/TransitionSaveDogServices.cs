using Service;

namespace SaveTheDog
{
    public class TransitionSaveDogServices : TransitionBaseServices
    {
        public static TransitionSaveDogServices instance = new TransitionSaveDogServices();

        public override string transitionPath => "prefab/transition/transition_save_the_dog";

        public override int closeAnimFrame => 60;
    }
}
