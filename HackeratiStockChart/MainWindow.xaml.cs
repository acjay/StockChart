using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private SortedList<DateTime, double> DJIA; 

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Event called when window is loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _dataset = new DataSeriesSet<DateTime, double>();
            StartDate = new DateTime(2000, 1, 1);
            EndDate = DateTime.Today;
            DJIA = GetDJIAQuotes();
            stockChart.DataSet = _dataset;
        }

        /// <summary>
        /// Handle click in the Load button to retrieve a quote
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Load_Click(object sender, RoutedEventArgs e)
        {
            // Create URL from text box data
            try
            {
                var movingAverage = Averaging.SelectedIndex > 0 ? Convert.ToInt32(Averaging.SelectionBoxItem) : 0;
                var entryName = StockSymbol.Text.ToUpper() + " (" + Averaging.SelectionBoxItem + ")";
                var quoteList = GetQuotesForSymbol(StockSymbol.Text, movingAverage);
                AddSeries(quoteList, entryName);
                SymbolsAdded.Items.Add(entryName);
            }
            catch (WebException)
            {
                MessageBox.Show("Stock symbol \"" + StockSymbol.Text + "\" could not be retrieved.");
            }
        }

        /// <summary>
        /// Handle a click to select a symbol to delete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SymbolsAdded_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Trace.WriteLine("Selection changed");

            // Control enabling of delete button with whether something has been selected
            Delete.IsEnabled = SymbolsAdded.SelectedIndex != -1;
        }

        /// <summary>
        /// Handle the click to delete a selected symbol from the chart
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            DeleteSeries(SymbolsAdded.SelectedItem.ToString());
            SymbolsAdded.Items.Remove(SymbolsAdded.SelectedItem);
        }

        /// <summary>
        /// Handle click on the checkbox to show/remove the DJIA
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowDJIA_Clicked(object sender, RoutedEventArgs e)
        {
            if (ShowDJIA.IsChecked == true)
            {
                AddSeries(DJIA, "DJIA");
            }
            else if (ShowDJIA.IsChecked == false)
            {
                DeleteSeries("DJIA");
            }
        }

        // Business logic

        /// <summary>
        /// Add data for a symbol to the chart
        /// </summary>
        /// <param name="quoteList">Retrieved quotes</param>
        /// <param name="seriesName">Name for the series (symbol + moving average length)</param>
        private void AddSeries(SortedList<DateTime, double> quoteList, string seriesName)
        {
            // Create a new data series
            var series = new XyDataSeries<DateTime, double>();
            series.Append(quoteList.Keys, quoteList.Values);
            series.SeriesName = seriesName;
            stockChart.DataSet.Add(series);

            // Display the data via RenderableSeries
            var renderSeries = new FastMountainRenderableSeries();
            stockChart.RenderableSeries.Add(renderSeries);

            stockChart.DataSet.InvalidateParentSurface(RangeMode.ZoomToFit);
            stockChart.ZoomExtents();
        }

        /// <summary>
        /// Remove data for a symbol from the chart
        /// </summary>
        /// <param name="seriesName">Name for the series (symbol + moving average length)</param>
        private void DeleteSeries(string seriesName)
        {
            // Find the series with the given name
            var rSeries = stockChart.RenderableSeries.Where(item => item.DataSeries.SeriesName == seriesName).First();

            // Delete it
            if (rSeries != null)
            {
                stockChart.DataSet.Remove(rSeries.DataSeries);
                stockChart.RenderableSeries.Remove(rSeries);

                // Re-order dataseries indices after remove
                // This caveat is discussed in detail at http://www.scichart.com/how-to-add-and-remove-chart-series-dynamically/
                // and is unique to v1.x of SciChart. In v2.0 we will be altering the DataSeries API to resolve this issue
                for (int i = 0; i < stockChart.RenderableSeries.Count; i++)
                {
                    stockChart.RenderableSeries[i].DataSeriesIndex = i;
                }

                stockChart.InvalidateElement();
            }
        }

        /// <summary>
        /// Retrieve quotes for a given symbol from the Internet
        /// </summary>
        /// <param name="symbol">Ticker symbol</param>
        /// <param name="movingAverage">Length of moving average window</param>
        /// <returns></returns>
        private SortedList<DateTime, double> GetQuotesForSymbol(string symbol, int movingAverage)
        {
            // Build URL to retrieve stock quote history from Yahoo
            var url = String.Format("http://ichart.yahoo.com/table.csv?s={0}&a={1}&b={2}&c={3}&d={4}&e={5}&f={6}&g=d&ignore=.csv",
                symbol, StartDate.Month - 1, StartDate.Day, StartDate.Year, EndDate.Month - 1, EndDate.Day, EndDate.Year);

            // Get a stream reader for the CSV-generating quote URL
            var quoteStream = new StreamReader(WebRequest.Create(url).GetResponse().GetResponseStream());
            return ParseQuotesFromStream(quoteStream, movingAverage, 4);
        }

        /// <summary>
        /// Get the quote list for the pre-downloaded DJIA data
        /// </summary>
        /// <returns>quote list</returns>
        private SortedList<DateTime, double> GetDJIAQuotes()
        {
            // Input the DJIA info from file
            var djiaStream = new StreamReader("DJIA.csv");
            var fullQuoteList = ParseQuotesFromStream(djiaStream, 0, 1);

            // Narrow down the DJIA info to the range we're interested in
            var quoteList = new SortedList<DateTime, double>();
            foreach (var quote in fullQuoteList)
            {
                if (quote.Key >= StartDate && quote.Key <= EndDate)
                {
                    quoteList.Add(quote.Key, quote.Value);
                }
            }
            return quoteList;
        }

        /// <summary>
        /// Parse stock quotes from a CSV stream
        /// 
        /// Date is assumed to be in YYYY-MM-DD format and in the first field
        /// </summary>
        /// <param name="quoteStream">CSV stream with headers</param>
        /// <param name="movingAverage">The length of the moving average window</param>
        /// <param name="indexForPrice">The index of the CSV field containing the price</param>
        /// <returns>quote list</returns>
        private SortedList<DateTime, double> ParseQuotesFromStream(StreamReader quoteStream, int movingAverage, int indexForPrice)
        {
            // Use the CSV reader to parse the dates and closing prices of the quotes
            var quoteList = new SortedList<DateTime, double>();
            using (CsvReader csv = new CsvReader(quoteStream, true))
            {
                double close = 0.0;
                while (csv.ReadNextRecord())
                {
                    // Parse out parts of date and retrive closing price
                    var dateParts = new List<int>(csv[0].Split('-').Select(x => Convert.ToInt32(x)));
                    var date = new DateTime(dateParts[0], dateParts[1], dateParts[2]);

                    // The index for the price is different for the Yahoo quotes and the DJIA text file
                    try
                    {
                        close = Convert.ToDouble(csv[indexForPrice]);
                    }
                    catch (FormatException)
                    {
                        // Just rely on the previous value
                    }

                    quoteList[date] = close;
                }
            }

            Trace.WriteLine(String.Format("quoteList length: {0}", quoteList.Count));

            // Store original start date before moving averages, to prevent alignment problems later
            var dataStartDate = quoteList.Keys[0];

            // Calculate moving average, if necessary
            if (movingAverage > 0)
            {
                quoteList = MovingAverage(quoteList, movingAverage);
            }

            Trace.WriteLine(String.Format("After MA: {0}", quoteList.Count));

            // All the code below is meant to cope with alignment errors in SciChart when plotting multiple data series on the same plot

            // Merge zeros into the retrieved data where it is undefine over the range (necessary because SciChart won't align data on its own)
            var curDate = StartDate;
            for (var i = 0; curDate < dataStartDate; i++, curDate = StartDate.AddDays(i))
            {
                quoteList[curDate] = 0.0;
            }

            // Make up for lag date
            curDate = dataStartDate;
            for (var i = 0; i < movingAverage; i++, curDate = dataStartDate.AddDays(i))
            {
                quoteList[curDate] = 0.0;
            }

            Trace.WriteLine(String.Format("After padding: {0}", quoteList.Count));

            return quoteList;
        }

        /// <summary>
        /// Computes the moving average of a quote list
        /// 
        /// Leaves off the first N-1 points before the window is fileld with data
        /// </summary>
        /// <param name="quoteList">The quote list to average</param>
        /// <param name="averageLength">The length of the moving average window</param>
        /// <returns>moving average quote list</returns>
        private SortedList<DateTime, double> MovingAverage(SortedList<DateTime, double> quoteList, int averageLength)
        {

            return quoteList.Skip(averageLength - 1).Aggregate(
                new
                {
                    // Initialize list to hold results
                    Result = new SortedList<DateTime, double>(),
                    // Initialize Working list with the first N-1 items
                    Working = new List<double>(quoteList.Take(averageLength - 1).Select(item => item.Value))
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
