using BRTDataTools.App.Models;
using BRTDataTools.App.PageModels;

namespace BRTDataTools.App.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}