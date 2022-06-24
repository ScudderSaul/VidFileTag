using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.MediaPlayerElement;
using LibVLCSharp.WPF;

namespace VidFileTag
{
    /// <summary>
    /// Interaction logic for ShowMediaInfo.xaml
    /// </summary>
    public partial class ShowMediaInfo : UserControl
    {

        private Dictionary<string, string> mediaInfo;
        private LibVLC _libVLC;

        // define an event handler
        public event PropertyChangedEventHandler PropertyChanged;

        public ShowMediaInfo()
        {
            InitializeComponent();
            _libVLC = new LibVLC();    // lib vlc
            DataContext = this;
        }

        public Dictionary<string, string> MediaInfo
            {
               get => mediaInfo;
              private set => Set(nameof(MediaInfo), ref mediaInfo, value);
        }



        public LibVLC LibVLC
        {
            get => _libVLC;
            private set => Set(nameof(LibVLC), ref _libVLC, value);
        }

        public void FillMediaInfo(string path)
        {

            mediaInfo = new Dictionary<string, string>();
            var media = new LibVLCSharp.Shared.Media(LibVLC, path);

            string inf = media.Meta(MetadataType.Title);
            if(inf != null)
            {
                mediaInfo.Add("Title", inf);
            }

            inf = media.Meta(MetadataType.Artist);
            if (inf != null)
            {
                mediaInfo.Add("Artist", inf);
            }
            inf = media.Meta(MetadataType.Genre);
            if (inf != null)
            {
                mediaInfo.Add("Genre", inf);
            }
            inf = media.Meta(MetadataType.Copyright);
            if (inf != null)
            {
                mediaInfo.Add("Copyright", inf);
            }
            inf = media.Meta(MetadataType.Album);
            if (inf != null)
            {
                mediaInfo.Add("Album", inf);
            }
            inf = media.Meta(MetadataType.TrackNumber);
            if (inf != null)
            {
                mediaInfo.Add("TrackNumber", inf);
            }
            inf = media.Meta(MetadataType.Description);
            if (inf != null)
            {
                mediaInfo.Add("Description", inf);
            }
            inf = media.Meta(MetadataType.Rating);
            if (inf != null)
            {
                mediaInfo.Add("Rating", inf);
            }
            inf = media.Meta(MetadataType.Date);
            if (inf != null)
            {
                mediaInfo.Add("Date", inf);
            }
            inf = media.Meta(MetadataType.Setting);
            if (inf != null)
            {
                mediaInfo.Add("Setting", inf);
            }
            inf = media.Meta(MetadataType.URL);
            if (inf != null)
            {
                mediaInfo.Add("URL", inf);
            }
            inf = media.Meta(MetadataType.Language);
            if (inf != null)
            {
                mediaInfo.Add("Language", inf);
            }
            inf = media.Meta(MetadataType.NowPlaying);
            if (inf != null)
            {
                mediaInfo.Add("NowPlaying", inf);
            }
            inf = media.Meta(MetadataType.Publisher);
            if (inf != null)
            {
                mediaInfo.Add("Publisher", inf);
            }
            inf = media.Meta(MetadataType.EncodedBy);
            if (inf != null)
            {
                mediaInfo.Add("EncodedBy", inf);
            }
            inf = media.Meta(MetadataType.ArtworkURL);
            if (inf != null)
            {
                mediaInfo.Add("ArtworkURL", inf);
            }
            inf = media.Meta(MetadataType.TrackID);
            if (inf != null)
            {
                mediaInfo.Add("TrackID", inf);
            }
            inf = media.Meta(MetadataType.TrackTotal);
            if (inf != null)
            {
                mediaInfo.Add("TrackTotal", inf);
            }
            inf = media.Meta(MetadataType.Director);
            if (inf != null)
            {
                mediaInfo.Add("Director", inf);
            }
            inf = media.Meta(MetadataType.Season);
            if (inf != null)
            {
                mediaInfo.Add("Season", inf);
            }
            inf = media.Meta(MetadataType.Episode);
            if (inf != null)
            {
                mediaInfo.Add("Episode", inf);
            }
            inf = media.Meta(MetadataType.ShowName);
            if (inf != null)
            {
                mediaInfo.Add("ShowName", inf);
            }
            inf = media.Meta(MetadataType.Actors);
            if (inf != null)
            {
                mediaInfo.Add("Actors", inf);
            }
            inf = media.Meta(MetadataType.AlbumArtist);
            if (inf != null)
            {
                mediaInfo.Add("AlbumArtist", inf);
            }
            inf = media.Meta(MetadataType.DiscNumber);
            if (inf != null)
            {
                mediaInfo.Add("DiscNumber", inf);
            }
            inf = media.Meta(MetadataType.DiscTotal);
            if (inf != null)
            {
                mediaInfo.Add("DiscTotal", inf);
            }
            MediaList.Items.Clear();
            foreach (var kk in mediaInfo.Keys)
            {
                string aa = string.Format(kk, mediaInfo[kk]);
                MediaList.Items.Add(aa);
                MediaList.InvalidateVisual();
                
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
