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
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.MediaPlayerElement;
using LibVLCSharp.WPF;

namespace VidFileTag
{
    /// <summary>
    /// Interaction logic for CntrlWindow.xaml
    /// </summary>
    public partial class CntrlWindow : Window
    {

        // define an event handler
        public event PropertyChangedEventHandler PropertyChanged;

        #region Fields

        string _filePath = string.Empty;
        private LibVLC _libVLC;
        private LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
       

        private float _positionSlider_value;

        bool MediaAdvanced = false;
        private VidWindow vid;
        private LibVLCSharp.Shared.MediaPlayer mediaPlayer;

        #endregion

        #region Ctor

        public CntrlWindow()
        {

            
            InitializeComponent();
            
            DataContext = this;

            ShowFileButton.Content = "Show \u25b6";
            PauseFileButton.Content = "Pause \u23F8";
            StopFileButton.Content = "Stop \u23F9";

            _libVLC = new LibVLC();    // lib vlc
            Closing += CntrlWindow_Closing;
        }

        #endregion

        #region Properties


        private VidWindow Vid { get => vid; set => vid = value; }

        public string FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                _filePath = value;
                Title = _filePath;
                ShowFileButton.IsEnabled = true;
                PauseFileButton.IsEnabled = true;
                StopFileButton.IsEnabled = true;
            }
        }

        public LibVLC LibVLC
        {
            get => _libVLC;
            private set => Set(nameof(LibVLC), ref _libVLC, value);
        }

        public float PositionSlider_Value
        {
            get
            {
                return _positionSlider_value;
            }
            set
            {
                _positionSlider_value = value;
                OnPropertyChanged("PositionSlider_Value");
            }
        }

        #endregion

        private void CntrlWindow_Closing(object sender, CancelEventArgs e)
        {
            if (MediaPlayer != null)
            {
                MediaPlayer.Stop();
            }
            if(Vid != null)
            {
               Vid.Close();
            }
            
            this.Hide();
        }


      //  LibVLCSharp.WPF.VideoView OtherView { get; set; } = null;

        private void ShowFileButton_Click(object sender, RoutedEventArgs e)
        {

            //   if (_VLCfileformats.Keys.Contains<string>(SelectedFileInfo.FileExtension.ToUpper()) == true)
            if (string.IsNullOrWhiteSpace(FilePath) == false)
            {
                if (Vid is null)
                {
                    Vid = new VidWindow();
                    Vid.Closing += Vid_Closing;
                }
                else
                {
                    Vid.Show();
                }

                var media = new LibVLCSharp.Shared.Media(LibVLC, FilePath);

                string titl = media.Meta(MetadataType.Title);

                if (MediaPlayer != null)
                {
                    MediaPlayer.Stop();
                }



                MediaPlayer = new LibVLCSharp.Shared.MediaPlayer(media) { EnableHardwareDecoding = true };

                Vid.RunMedia(MediaPlayer, FilePath);

              //  MediaPlayer = OtherView.MediaPlayer;

                MediaPlayer.PositionChanged += MediaPlayer_PositionChanged;

                MediaPlayer.Play();

                MediaPlayer.EnableKeyInput = true;

                PositionSlider.IsEnabled = true;
                PositionSlider.Value = MediaPlayer.Position; // (float)0.0;

                VolumeSlider.IsEnabled = true;
                VolumeSlider.Value = MediaPlayer.Volume;

            }

        }

        private void Vid_Closing(object sender, CancelEventArgs e)
        {
            MediaPlayer.Stop();
            Vid = null;
        }

        LibVLCSharp.Shared.MediaPlayer MediaPlayer { get => mediaPlayer; set => mediaPlayer = value; }

        private void MediaPlayer_PositionChanged(object sender, MediaPlayerPositionChangedEventArgs e)
        {
            MediaAdvanced = true;


            OnPositionSlider_ValueChanged(sender, e.Position);
        }

        private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MediaAdvanced == true)
            {
                MediaAdvanced = false;
                InvalidateArrange();
                InvalidateVisual();
                return;
            }


            if (MediaPlayer != null && MediaPlayer.IsPlaying)
            {
                double dd = PositionSlider.Value;
                MediaPlayer.Position = (float)dd;
            }
        }

        void OnPositionSlider_ValueChanged(object it, float ff)
        {
            try
            {
                MediaAdvanced = true;
                PositionSlider_Value = ff;
            }
            catch (Exception e)
            {
                string rr = e.Message;
            }
        }

        private void PauseFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (MediaPlayer != null)
            {
                MediaPlayer.Pause();
            }

        }

        private void StopFileButton_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer.Stop();
            Vid.Hide();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MediaPlayer != null && MediaPlayer.IsPlaying)
            {
                double dd = VolumeSlider.Value;
                MediaPlayer.Volume = (int)dd;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MediaPlayer.Stop();
            vid.Close();
            Vid = null;
            this.Hide();
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

