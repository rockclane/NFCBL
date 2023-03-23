using NFCBL.ViewModels;
using System.ComponentModel;
using Xamarin.Forms;

namespace NFCBL.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        public ItemDetailPage()
        {
            InitializeComponent();
            BindingContext = new ItemDetailViewModel();
        }
    }
}