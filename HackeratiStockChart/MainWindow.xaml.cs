using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Abt.Controls.SciChart;
using Abt.Controls.SciChart.Utility;

namespace HackeratiStockChart
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var dataset = new DataSeriesSet<DateTime, double>();
            var series = dataset.AddSeries();

            var startDate = new DateTime(2000, 1, 1);

            for (int i = 0; i < 4000; i++)
            {
                var curDate = startDate.AddDays(i);
                series.Append(curDate, 25.0 + 5.0 * Math.Sin(2 * Math.PI * i / 1000));
            }

            stockChart.DataSet = dataset;

        }
    }
}
