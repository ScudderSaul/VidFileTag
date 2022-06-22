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
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Xml.Linq;
using LibVLCSharp.Shared;


namespace VidFileTag
{
    /// <summary>
    /// Interaction logic for VidWindow.xaml
    /// </summary>
    public partial class VidWindow : Window
    {
        public VidWindow()
        {
            InitializeComponent();
        }

        LibVLCSharp.Shared.MediaPlayer MediaPlayer { get; set; }

        public void RunMedia(LibVLCSharp.Shared.MediaPlayer mediaPlayer, string path)
        {
            this.Title = path;
            MediaPlayer = mediaPlayer;

            var wwindow = System.Windows.Window.GetWindow(this);
            MediaPlayer.Hwnd = new WindowInteropHelper(wwindow).EnsureHandle();

            MediaPlayer.Play();

            this.Show();
        }

    }
}
