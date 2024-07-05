namespace ShortLists
{
    public partial class MainPage : ContentPage
    {

        public List<Fruit> Fruits { get; set; } = new List<Fruit>
        {
            new Fruit { FruitName = "Apple", Price = 1.99 },
            new Fruit { FruitName = "Orange", Price = 0.79 },
            new Fruit { FruitName = "Banana", Price = 0.49 },
            // Add more fruits here
        };

        int count = 0;


        public MainPage()
        {
            InitializeComponent();
            FruitListView.ItemsSource = Fruits;
        }


        private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            if (sender is Label label)
            {
                // Find the associated CheckBox
                var fruit = label.BindingContext as Fruit; // Replace with your data model type
                if (fruit != null)
                {
                    fruit.IsCrossedOut = !fruit.IsCrossedOut; // Toggle the IsCrossedOut property
                }
            }
        }

        //private void OnCounterClicked(object sender, EventArgs e)
        //{
        //    count++;

        //    if (count == 1)
        //        CounterBtn.Text = $"Clicked {count} time";
        //    else
        //        CounterBtn.Text = $"Clicked {count} times";

        //    SemanticScreenReader.Announce(CounterBtn.Text);
        //}
    }
    public class Fruit
    {
        public string FruitName { get; set; }
        public double Price { get; set; }
        // Add other properties like color, weight, etc. as needed
        public bool IsCrossedOut { get; set; }

    }

}
