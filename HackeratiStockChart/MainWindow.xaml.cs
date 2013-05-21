using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
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
using LumenWorks.Framework.IO.Csv;

namespace HackeratiStockChart
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DataSeriesSet<DateTime, double> _dataset;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _dataset = new DataSeriesSet<DateTime, double>();
            stockChart.DataSet = _dataset;

            // Add a dummy series to start with (prevents future errors)
            var dummyQuoteList = new SortedList<DateTime, double>();
            var startDate = new DateTime(2010, 1, 1);
            for (int i = 0; i < 1000; i++)
            {
                dummyQuoteList.Add(startDate.AddDays(i), 0.0);
            }
            AddSeries(dummyQuoteList, 0);
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            // Create URL from text box data
            var quoteList = GetQuotes(StockSymbol.Text);
            AddSeries(quoteList, Averaging.SelectedIndex);
        }

        private void AddSeries(SortedList<DateTime, double> quoteList, int averagingType)
        {
            // Create a new data series
            var series = new XyDataSeries<DateTime, double>();
            series.Append(quoteList.Keys, quoteList.Values);
            stockChart.DataSet.Add(series);

            // Display the data via RenderableSeries
            var renderSeries = new FastMountainRenderableSeries();
            stockChart.RenderableSeries.Add(renderSeries);
            stockChart.ZoomExtents();
        }

        private SortedList<DateTime, double> GetQuotes(string symbol)
        {
            // Build URL to retrieve stock quote history from Yahoo
            var url = "http://ichart.yahoo.com/table.csv?s=" + symbol + "&a=0&b=1&c=2000&d=0&e=31&f=2013&g=d&ignore=.csv";

            // Get a stream reader for the CSV-generating quote URL
            var quoteStream = new StreamReader(WebRequest.Create(url).GetResponse().GetResponseStream());

            // Use the CSV reader to parse the dates and closing prices of the quotes
            var quoteList = new SortedList<DateTime, double>();
            using (CsvReader csv = new CsvReader(quoteStream, true))
            {
                while (csv.ReadNextRecord())
                {
                    // Parse out parts of date and retrive closing price
                    var dateParts = new List<int>(csv[0].Split('-').Select(x => Convert.ToInt32(x)));
                    var date = new DateTime(dateParts[0], dateParts[1], dateParts[2]);
                    var close = Convert.ToDouble(csv[4]);
                    quoteList.Add(date, close);
                }
            }

            // Reshape -- convert list of tuples to tuple of lists
            return quoteList;
        }
    }
}
