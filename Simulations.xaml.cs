using RunningCost.Models;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using SkiaSharp;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using CommunityToolkit.Mvvm.ComponentModel;
using AlgoTrade.Shared.Enums;
using AlgoTrade.Shared;
using AlgoTrade.Shared.ApiModels;
using System.Threading.Tasks;
using Syncfusion.Maui.DataSource.Extensions;
using static RunningCost.Pages.Main.ViewModel;

namespace RunningCost.Pages.Main;

//Ref: https://livecharts.dev/docs/Eto/2.0.0-rc2/gallery

public partial class Simulations : ContentPage
{
    string currentTickerName = string.Empty;
    public Simulations()
	{
        InitializeComponent();
        tickerSelection.TickerSelected += TickerSelection_TickerSelected;
        
       
        displayStartPicker.OkButtonClicked += (s, e) =>
        {
            displayStartBtn.Text = displayStartPicker.SelectedDate.Value.ToString("MM/dd/yyyy hh:mm:ss tt");
            displayStartPicker.IsOpen = false;
        };
        displayStartPicker.CancelButtonClicked += (s, e) =>
        {
            displayStartPicker.IsOpen = false;
        };

        simStartPicker.OkButtonClicked += (s, e) =>
        {
            simStartBtn.Text = simStartPicker.SelectedDate.Value.ToString("MM/dd/yyyy hh:mm:ss tt");
            simStartPicker.IsOpen = false;
        };
        simStartPicker.CancelButtonClicked += (s, e) =>
        {
            simStartPicker.IsOpen = false;
        };

        simEndPicker.OkButtonClicked += (s, e) =>
        {
            simEndBtn.Text = simEndPicker.SelectedDate.Value.ToString("MM/dd/yyyy hh:mm:ss tt");
            simEndPicker.IsOpen = false;
        };
        simEndPicker.CancelButtonClicked += (s, e) =>
        {
            simEndPicker.IsOpen = false;
        };

        //assign defaults.
        tickerSelection.UpdateSelectedTicker("TSLA");
        historical.SelectedIndex = 14; //assign Hr1 index.
        maItems.SelectedItems = new ObservableCollection<SelectionModel<double>>()
        {
            ViewModel.ModelInstance.MovingAverages[3],
            ViewModel.ModelInstance.MovingAverages[5]
        };

        simStartPicker.SelectedDate = DateTime.Parse("2021-01-01T00:00:00"); // DateTime.Now.AddDays(-7);
        simStartPicker.MinimumDate = simStartPicker.SelectedDate.Value;
        simStartPicker.MaximumDate = DateTime.Now;
        simStartBtn.Text = simStartPicker.SelectedDate.Value.ToString("MM/dd/yyyy hh:mm:ss tt");

        simEndPicker.SelectedDate = DateTime.Now;
        simEndPicker.MinimumDate = simStartPicker.SelectedDate.Value; 
        simEndPicker.MaximumDate = DateTime.Now;
        simEndBtn.Text = simEndPicker.SelectedDate.Value.ToString("MM/dd/yyyy hh:mm:ss tt");
        displayStartBtn.IsEnabled = false;
        displayStartBtn.Text = "";
        displayRange.ItemsSource = ViewModel.ModelInstance.DisplayRangeFiltered;

        //tickerSelection.TickerSelected += TickerSelection_TickerSelected;
        //tickerSelection.TickerRecalculated += TickerSelection_TickerRecalculated;
        //BindingContext = new GraphViewModel();
        //CSApiClient.GetData().ContinueWith((task) =>
        //{
        //    if (task.IsCompletedSuccessfully == true)
        //    {
        //        var data = task.Result;
        //        var result = JsonConvert.DeserializeObject<List<GetHistoricalsApiModel>>(data);
        //        if (result != null && result.Count > 0)
        //        {
        //            this.Dispatcher.Dispatch(() =>
        //            {
        //                ((GraphViewModel)BindingContext).Series[0].Values = new List<DateTimePoint>(result.Select(x => new DateTimePoint(x.UpdatedAt, x.HistoricalPrice)));
        //                ((GraphViewModel)BindingContext).Series[1].Values = new List<DateTimePoint>(result.Select(x => new DateTimePoint(x.UpdatedAt, x.MovingAveragePrice)));
        //            });
        //        }
        //    }
        //});
    }

    private void TickerSelection_TickerSelected(object? sender, TickerSelectionModel e)
    {
        currentTickerName = e.SelectedTicker;
    }

    private void OpenDisplayStartDate(object sender, EventArgs e)
    {
        displayStartPicker.IsOpen = true;
    }

    
      private void OpenSimStartDate(object sender, EventArgs e)
    {
        simStartPicker.IsOpen = true;
    }


    private void OpenSimEndDate(object sender, EventArgs e)
    {
        simEndPicker.IsOpen = true;
    }

    private void RunSimulation(object sender, EventArgs e)
    {
        if (currentTickerName == string.Empty)
        {
            this.DisplayAlert("Error", "Select Ticker  First", "OK");
            return;
        }
        if (historical.SelectedItem == null)
        {
            this.DisplayAlert("Error", "Select Historical Interval", "OK");
            return;
        }
        if (maItems.SelectedItems == null || maItems.SelectedItems.Count == 0)
        {
            this.DisplayAlert("Error", "Select Moving Average(s)", "OK");
            return;
        }

        RunSimulationApiModel model = new RunSimulationApiModel();
        var ticker = new TickerDataApiModel()
        {
            TickerSymbol = currentTickerName,
            MAIntervals = new List<MAHistoricalMapModel>()
        };
        foreach (var item in maItems.SelectedItems)
        {
            ticker.MAIntervals.Add(new MAHistoricalMapModel()
            {
                HistoricalInterval = (TimeUnitsEnum)((SelectionModel<double>)historical.SelectedItem).RefID,
                MAInterval = (MovingAverageEnum)((SelectionModel<double>)item).RefID,
                IsEnabled = true
            });
        }
        model.TickerSimData?.Add(ticker);
        model.StartTimeStamp = DateTime.Parse(simStartBtn.Text);
        model.EndTimeStamp = DateTime.Parse(simEndBtn.Text);

        CSApiClient.RunSimulation(model).ContinueWith((task) =>
        {
            if (task.IsCompletedSuccessfully == true)
            {
                var rslt = task.Result;
                if (rslt is "{}" or null or "")
                {
                    this.DisplayAlert("Error", "Simulation Failed", "OK");
                    return;
                }
                var result = JsonConvert.DeserializeObject<List<GetHistoricalsApiModel>>(rslt);
                if (result != null && result.Count > 0)
                {
                    this.Dispatcher.Dispatch(() =>
                    {
                        SortedDictionary<DateTime, SimulationMovingAveragesModelItem> data = JsonConvert.DeserializeObject<SortedDictionary<DateTime, SimulationMovingAveragesModelItem>>(rslt);
                        if (data != null && data.Count > 0)
                        {
                            ModelInstance.ChartDataAll.Clear();
                            ModelInstance.ChartLinesCount = data.First().Value.MovingAveragePrice.Count;
                            foreach (var item in data)
                            {
                                if (item.Value.MovingAveragePrice.Count != ModelInstance.ChartLinesCount)
                                {
                                    this.DisplayAlert("Error", "Moving Average Count Mismatch", "OK");
                                    return;
                                }
                                var sorted = item.Value.MovingAveragePrice.OrderBy(x => x.MovingAverageMode).ToList();
                                double? MovingAverageV2 = null;
                                double? MovingAverageV3 = null;
                                double? MovingAverageV4 = null;
                                double MovingAverageV1 = sorted[0].MovingAveragePrice;
                                if (item.Value.MovingAveragePrice.Count > 1)
                                {
                                    MovingAverageV2 = sorted[1].MovingAveragePrice;
                                }
                                if (item.Value.MovingAveragePrice.Count > 2)
                                {
                                    MovingAverageV3 = sorted[2].MovingAveragePrice;
                                }
                                if (item.Value.MovingAveragePrice.Count > 3)
                                {
                                    MovingAverageV4 = sorted[3].MovingAveragePrice;
                                }

                                ModelInstance.ChartDataAll.Add(new ChartModel(item.Key, item.Value.HistoricalPrice, MovingAverageV1, MovingAverageV2, MovingAverageV3, MovingAverageV4));
                            }
                            ModelInstance.ProcessChartDataColors();
                            displayStartPicker.SelectedDate = data.First().Value.IntervalStartTime;
                            displayStartPicker.MinimumDate = data.First().Value.IntervalStartTime;
                            displayStartPicker.MaximumDate = data.Last().Value.IntervalEndTime;
                            displayStartBtn.Text = displayStartPicker.SelectedDate.Value.ToString("MM/dd/yyyy hh:mm:ss tt");
                            displayStartBtn.IsEnabled = true;
                            ModelInstance.DisplayRangeFiltered.Clear();
                            ModelInstance.DisplayRangeAll.Where(x => x.RefID >= ((SelectionModel<double>)historical.SelectedItem).RefID).ForEach(x => ModelInstance.DisplayRangeFiltered.Add(x));
                            ModelInstance.DisplayIntervalFiltered.Clear();
                            ModelInstance.DisplayIntervalAll.Where(x => x.RefID >= ((SelectionModel<double>)historical.SelectedItem).RefID).ForEach(x => ModelInstance.DisplayIntervalFiltered.Add(x));
                            displayRange.SelectedIndex = 0;
                            displayInterval.SelectedItem = ModelInstance.DisplayIntervalFiltered.FirstOrDefault(x => x.DisplayValue == "1 Week");
                            if(displayInterval.SelectedItem == null) displayInterval.SelectedIndex = 0;

                            tabView.SelectedIndex = 1;
                        }
                    });
                }
            }
            else
            {
                this.DisplayAlert("Error", "Unexpected Error. Simulation Failed.", "OK");
                return;
            }
        });
    }

    private void ChartSelectionChanged(object sender, Syncfusion.Maui.Charts.ChartSelectionChangedEventArgs e)
    {

    }
}

public class ChartModel
{
    public DateTime TimeLine { get; set; }
    public double Historical { get; set; }
    public double MAPriceV1 { get; set; }
    public double MAPriceV2 { get; set; }
    public double MAPriceV3 { get; set; }
    public double MAPriceV4 { get; set; }
    public TransactionTypeEnum LastTransactionType { get; set; }
    public double? LastTransactionAmt { get; set; }
    public ChartModel(DateTime timeLine, double historical,double MovingAverageV1, double? MovingAverageV2, double? MovingAverageV3, double? MovingAverageV4)
    {
        TimeLine = timeLine;
        Historical = historical;
        MAPriceV1 = MovingAverageV1;
        MAPriceV2 = MovingAverageV2 ?? 0;
        MAPriceV3 = MovingAverageV3 ?? 0;
        MAPriceV4 = MovingAverageV4 ?? 0;
    }
}

public class ViewModel
{
    public static ViewModel ModelInstance { get; set; }
    public ObservableCollection<SelectionModel<double>> MovingAverages { get; set; }
    public ObservableCollection<ChartModel> ChartDataAll { get; set; } = new ObservableCollection<ChartModel>();
    public ObservableCollection<ChartModel> ChartDataFiltered { get; set; } = new ObservableCollection<ChartModel>();
    public int ChartLinesCount { get; set; }
    public bool LineMA1Visible { get { return ChartLinesCount >= 1;  } }
    public bool LineMA2Visible { get { return ChartLinesCount >= 2; } }
    public bool LineMA3Visible { get { return ChartLinesCount >= 3; } }
    public bool LineMA4Visible { get { return ChartLinesCount >= 4; } }
    public bool TradeLineVisible { get; set; } = false;
    public ObservableCollection<SelectionModel<double>> DisplayRangeAll { get; set; } = new ObservableCollection<SelectionModel<double>>();

    public ObservableCollection<SelectionModel<double>> DisplayRangeFiltered { get; set; } = new ObservableCollection<SelectionModel<double>>();

    public ObservableCollection<SelectionModel<double>> DisplayIntervalAll { get; set; } = new ObservableCollection<SelectionModel<double>>();

    public ObservableCollection<SelectionModel<double>> DisplayIntervalFiltered { get; set; } = new ObservableCollection<SelectionModel<double>>();

    public ObservableCollection<SelectionModel<double>> HistoricalRange { get; set; }

    public ObservableCollection<Brush> CustomBrushes { get; set; } = new ObservableCollection<Brush>();

    public bool ProcessChartDataColors()
    {
        CustomBrushes.Clear();
        double min, mid, max;
        foreach (var item in ChartDataFiltered)
        {
            if(item.LastTransactionType is TransactionTypeEnum.MarketBuy or TransactionTypeEnum.LimitBuy)
            {
                CustomBrushes.Add(new SolidColorBrush(Colors.Red));
            }
            else
                CustomBrushes.Add(new SolidColorBrush(Colors.Green));
        }
        return true;
    }

    public ViewModel()
    {
        this.MovingAverages = EnumExtensions.ConvertEnumToSelection<MovingAverageEnum>();
        this.HistoricalRange = EnumExtensions.ConvertEnumToSelection<TimeUnitsEnum>();
        this.HistoricalRange.RemoveAt(0); //remove Sec1 since it is too short for historical data.
        this.HistoricalRange.RemoveAt(0); //remove Sec2 since it is too short for historical data.
        this.DisplayRangeAll = new ObservableCollection<SelectionModel<double>>()
        {
            new SelectionModel<double>("5 Seconds", 5),
            new SelectionModel<double>("10 Seconds", 10),
            new SelectionModel<double>("15 Seconds", 15),
            new SelectionModel<double>("30 Seconds", 30),
            new SelectionModel<double>("45 Seconds", 45),
            new SelectionModel<double>("1 Min", 60),
            new SelectionModel<double>("5 Min", 300),
            new SelectionModel<double>("10 Min", 600),
            new SelectionModel<double>("15 Min", 900),
            new SelectionModel<double>("30 Min", 1800),
            new SelectionModel<double>("45 Min", 2700),
            new SelectionModel<double>("1 Hr", 3600),
            new SelectionModel<double>("2 Hr", 7200),
            new SelectionModel<double>("3 Hr", 10800),
            new SelectionModel<double>("6 Hr", 21600),
            new SelectionModel<double>("12 Hr", 43200),
            new SelectionModel<double>("1 Day", 86400),
            new SelectionModel<double>("2 Day", 172800),
            new SelectionModel<double>("3 Day", 259200),
            new SelectionModel<double>("1 Week", 604800),
            new SelectionModel<double>("2 Week", 1209600),
            new SelectionModel<double>("1 Month", 2592000),
            new SelectionModel<double>("3 Month", 7776000),
            new SelectionModel<double>("1 Year", 31104000),
        };
        DisplayRangeAll.ForEach(x => DisplayRangeFiltered.Add(x));
        DisplayIntervalAll = new ObservableCollection<SelectionModel<double>>()
        {
            new SelectionModel<double>("30 Seconds", 30),
            new SelectionModel<double>("1 Min", 60),
            new SelectionModel<double>("30 Min", 1800),
            new SelectionModel<double>("1 Hr", 3600),
            new SelectionModel<double>("3 Hr", 10800),
            new SelectionModel<double>("6 Hr", 21600),
            new SelectionModel<double>("12 Hr", 43200),
            new SelectionModel<double>("1 Day", 86400),
            new SelectionModel<double>("3 Day", 259200),
            new SelectionModel<double>("1 Week", 604800),
            new SelectionModel<double>("1 Month", 2592000),
            new SelectionModel<double>("3 Month", 7776000),
            new SelectionModel<double>("1 Year", 31104000),
        };
        ModelInstance = this;
    }
    public enum TransactionTypeEnum
    {
        None=0,
        MarketBuy = 1,
        MarketSell=2,
        LimitBuy=3,
        Limitsell=4
    }
}
