using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniRx;
using CommonILRuntime.Module;
using CommonILRuntime.BindingModule;

namespace Services
{
    public class UIHideBackServices
    {
        static UIHideBackServices _instance = new UIHideBackServices();
        public static UIHideBackServices Instance { get { return _instance; } }

        Subject<int> nowTopScore = new Subject<int>();
        List<ContainerPresenter> fullyOccupyContainers = new List<ContainerPresenter>();

        List<ContainerPresenter> waitAddList = new List<ContainerPresenter>();

        public async void waitAddContainer(ContainerPresenter container, float waitAddTime = 0.5f)
        {
            if (!waitAddList.Contains(container))
                waitAddList.Add(container);

            await Task.Delay(TimeSpan.FromSeconds(waitAddTime));

            addContainer(container);
        }

        private void addContainer(ContainerPresenter container)
        {
            if (!waitAddList.Contains(container))
                return;

            if (!fullyOccupyContainers.Contains(container))
                fullyOccupyContainers.Add(container);

            checkAndNotifyScore();
        }

        public void removeContainer(ContainerPresenter container)
        {
            waitAddList.Remove(container);
            fullyOccupyContainers.Remove(container);

            checkAndNotifyScore();
        }

        public IDisposable subscribe(Action<int> nowTopScoreNotify)
        {
            return nowTopScore.Subscribe(nowTopScoreNotify);
        }

        private void checkAndNotifyScore()
        {
            int topScore = -1;

            for (int i = fullyOccupyContainers.Count - 1; i >= 0; i--)
            {
                if (fullyOccupyContainers[i].uiGameObject == null)
                {
                    fullyOccupyContainers.RemoveAt(i);
                    continue;
                }

                int score = CoculateContainerLayerScore(fullyOccupyContainers[i]);

                if (score >= topScore)
                    topScore = score;
            }

            nowTopScore.OnNext(topScore);
        }

        public static int CoculateContainerLayerScore(ContainerPresenter container)
        {
            int containerIndex = container.uiTransform.GetSiblingIndex();
            int rootIndex = container.getUiParent().parent.GetSiblingIndex();

            return rootIndex * 1000 + containerIndex;
        }
    }
}
