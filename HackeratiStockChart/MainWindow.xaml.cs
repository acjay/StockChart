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

        private DateTime StartDate;
        private DateTime EndDate;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _dataset = new DataSeriesSet<DateTime, double>();
            StartDate = new DateTime(2000, 1, 1);
            EndDate = DateTime.Today;
            stockChart.DataSet = _dataset;
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            // Create URL from text box data
            try
            {
                var quoteList = GetQuotes(StockSymbol.Text, Averaging.SelectedIndex);
                AddSeries(quoteList);
            }
            catch (WebException)
            {
                MessageBox.Show("Stock symbol \"" + StockSymbol.Text + "\" could not be retrieved.");
            }
        }

        private void AddSeries(SortedList<DateTime, double> quoteList)
        {
            // Create a new data series
            var series = new XyDataSeries<DateTime, double>();
            series.Append(quoteList.Keys, quoteList.Values);
            stockChart.DataSet.Add(series);

            // Display the data via RenderableSeries
            var renderSeries = new FastMountainRenderableSeries();
            stockChart.RenderableSeries.Add(renderSeries);

            stockChart.DataSet.InvalidateParentSurface(RangeMode.ZoomToFit);
            stockChart.ZoomExtents();
        }

        private SortedList<DateTime, double> GetQuotes(string symbol, int averagingType)
        {
            // Build URL to retrieve stock quote history from Yahoo
            var url = String.Format("http://ichart.yahoo.com/table.csv?s={0}&a={1}&b={2}&c={3}&d={4}&e={5}&f={6}&g=d&ignore=.csv",
                symbol, StartDate.Month - 1, StartDate.Day, StartDate.Year, EndDate.Month - 1, EndDate.Day, EndDate.Year);

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
                    quoteList[date] = close;
                }
            }

            // Calculate moving average, if necessary
            if (averagingType > 0)
            {
                int[] averageTypes = {0, 50, 100, 200};
                var averageLength = averageTypes[averagingType];
                quoteList = MovingAverage(quoteList, averageLength);
            }

            // Merge zeros into the retrieved data where it is undefine over the range (necessary because SciChart won't align data on its own)
            var curDate = StartDate;
            for (var i = 0; curDate <= EndDate; i++, curDate = StartDate.AddDays(i))
            {
                if (!quoteList.ContainsKey(curDate))
                {
                    quoteList[curDate] = 0.0;
                }
                else
                {
                    break;
                }
            }

            return quoteList;
        }

        private SortedList<DateTime, double> MovingAverage(SortedList<DateTime, double> startList, int averageLength)
        {

            return startList.Skip(averageLength - 1).Aggregate(
                new
                {
                    // Initialize list to hold results
                    Result = new SortedList<DateTime, double>(),
                    // Initialize Working list with the first N-1 items
                    Working = new List<double>(startList.Take(averageLength - 1).Select(item => item.Value))
                },
                (list, item) =>
                {
                    // Add the price from the current date to the working list
                    list.Working.Add(item.Value);
                    // Calculate the N item average for the past N days and add to the result list for the current date
                    list.Result.Add(item.Key, list.Working.Average());
                    // Remove the item from N days ago
                    list.Working.RemoveAt(0);
                    return list;
                }
            ).Result;
        }
    }
}
