namespace HeatController
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Dark : ResourceDictionary
    {
        public Dark()
        {
            InitializeComponent();
            MergedDictionaries.Add(new Styles());
        }
    }
}