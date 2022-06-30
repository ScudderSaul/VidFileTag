using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VidFileTag
{
    /// <summary>
    /// Interaction logic for SearchInFilename.xaml
    /// </summary>
    public partial class SearchInFilename : UserControl
    {
        bool _firstOne = false;

        bool _lastOne = false;

        bool _phrase = false;

        bool _custom = false;

        string? _fullTag = null;
        
        public SearchInFilename()
        {
            InitializeComponent();
            DataContext = this;
        }

        public string? FullTag 
        { 
            get { return _fullTag; } 
            set 
            {
                _fullTag = value;
                TagString.Text = value;
                FirstOne = true;
            }

        }

        public bool FirstOne { 
            get
            {
                return _firstOne;
            } 
            set
            {
               _firstOne = value;
               
               if(value)
                {
                    string[] partsoftag;
                    if (_fullTag != null)
                    {
                        partsoftag = _fullTag.Split(' ', 5, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        FirstOneCheck.IsChecked = true;
                        LastOneCheck.IsChecked = false;
                        PhraseCheck.IsChecked = false;
                        CustomCheck.IsChecked = false;
                        SearchText.Text = partsoftag[0];
                    }
                }
            } 
        }

        public bool LastOne
        {
            get
            {
                return _lastOne;
            }
            set
            {
                _lastOne = value;
               
                if (value)
                {
                    string[] partsoftag;
                    if (_fullTag != null)
                    {
                        partsoftag = _fullTag.Split(' ', 5, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        LastOneCheck.IsChecked = true;
                        FirstOneCheck.IsChecked = false;
                        PhraseCheck.IsChecked = false;
                        CustomCheck.IsChecked = false;
                        SearchText.Text =  partsoftag.Last();
                    }
                }
            }
        }

        public bool Phrase
        {
            get 
            {
                return _phrase;
            }
            set 
            { 
                _phrase = value;
                if (value)
                {
                   
                    if (_fullTag != null)
                    {
                        LastOneCheck.IsChecked = false;
                        FirstOneCheck.IsChecked = false;
                        PhraseCheck.IsChecked = true;
                        CustomCheck.IsChecked = false;
                        SearchText.Text =  _fullTag;
                    }
                }
            }
        }

       public bool Custom
        {

            get
            {
                return _custom;
            }
            set
            {
                _custom = value;
                if (value)
                {
                    if (_fullTag != null)
                    {
                         LastOneCheck.IsChecked = false;
                        FirstOneCheck.IsChecked = false;
                        PhraseCheck.IsChecked = false;
                        CustomCheck.IsChecked = true;
                      //  SearchText.Text =  ;
                    }
                }
            }
        }

       


        private void FirstOneCheck_Checked(object sender, RoutedEventArgs e)
        {
            FirstOne = true;
        }

        private void LastOneCheck_Checked(object sender, RoutedEventArgs e)
        {
            LastOne = true;
        }

        private void PhraseCheck_Checked(object sender, RoutedEventArgs e)
        {
            Phrase = true;
        }

        private void CustomCheck_Checked(object sender, RoutedEventArgs e)
        {
            Custom = true;
        }

        public void DoSearch_Click(object sender, RoutedEventArgs e)
        {

        }

        private void NoSearch_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
