using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace VidFileTag
{


  
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        // define an event handler
        public event PropertyChangedEventHandler PropertyChanged;

        public AboutWindow()
        {
            InitializeComponent();

            DataContext = this;
            Closing += AboutWindow_Closing;
        }

        private void AboutWindow_Closing(object sender, CancelEventArgs e)
        {
            this.Hide();
        }

        private void AboutClose_Click(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Collapsed;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //   Process.Start(((Hyperlink)sender).NavigateUri.ToString());
            }
            catch (Exception ee)
            {
                string not = ee.Message;
            }

        }

        private void Hyperlink1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //   Process.Start(((Hyperlink)sender).NavigateUri.ToString());
            }
            catch (Exception ee)
            {
                string not = ee.Message;
            }

        }

        private void Set<T>(string propertyName, ref T field, T value)
        {
            if (field == null && value != null || field != null && !field.Equals(value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
