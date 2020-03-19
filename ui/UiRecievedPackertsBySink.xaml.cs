using TreeBased.Dataplane;
using System.Windows;

namespace TreeBased.ui
{
    /// <summary>
    /// Interaction logic for UiRecievedPackertsBySink.xaml
    /// </summary>
    public partial class UiRecievedPackertsBySink : Window
    {
       
        public UiRecievedPackertsBySink()
        {
            InitializeComponent();
            dg_packets.ItemsSource = PublicParameters.FinishedRoutedPackets;
        }
    }
    
}
