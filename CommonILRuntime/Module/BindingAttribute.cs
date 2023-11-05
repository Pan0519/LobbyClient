using System;

namespace Module.Binding
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BindingPresenter : Attribute
    {
        public string uiPath { get; private set; }
        public UiLoadFrom uiLoadFrom { get; private set; }
        public UiLayer uiLayer { get; private set; }

        public UiLoadFile uiLoadFile { get; private set; }

        public BindingPresenter(string uiPath, UiLoadFrom uiLoadFrom = UiLoadFrom.Resources, UiLayer uiLayer = UiLayer.Default, UiLoadFile loadFile = UiLoadFile.GameArt)
        {
            this.uiPath = uiPath;
            this.uiLoadFrom = uiLoadFrom;
            this.uiLayer = uiLayer;
            this.uiLoadFile = loadFile;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class BindingField : Attribute
    {
        public string identifier { get; private set; }

        public BindingField(string identifier)
        {
            this.identifier = identifier;
        }
    }
}
