using Microsoft.Maui.Dispatching;
using SongPrompter.ViewModels;
using System.Diagnostics;

namespace SongPrompter
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            this.BindingContext = viewModel;
        }
    }
}
