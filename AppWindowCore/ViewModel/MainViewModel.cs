using CommunityToolkit.Mvvm.ComponentModel;

namespace AppWindowCore.ViewModel
{
    public class MainViewModel : ObservableObject
    {
        string _name;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
    }
}
