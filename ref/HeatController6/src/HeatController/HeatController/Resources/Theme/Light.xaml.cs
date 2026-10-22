namespace HeatController
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Light : ResourceDictionary
    {
        public Light()
        {
            InitializeComponent();
            MergedDictionaries.Add(new Styles());
        }
    }
}