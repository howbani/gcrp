using TreeBased.Charts;
using TreeBased.Intilization;
using TreeBased.ExpermentsResults;
using System.Collections.Generic;
using System.Windows;

namespace TreeBased.ui
{
    /// <summary>
    /// Interaction logic for ExpermentResults.xaml
    /// </summary>
    public partial class UIExpermentResults : Window 
    {
        public UIExpermentResults() 
        {
            InitializeComponent();
            /*
            List<CompareResult> list = new List<CompareResult>();
            CompareResult res = CompareResultsClass.GetEnergyDelayForAnExperment();
            list.Add(res);
            dg_data.ItemsSource = list;*/
        }
    }
}
