using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.MediaPlayerElement;
using LibVLCSharp.WPF;

namespace VidFileTag
{
    public class MediaInfo
    {
        //     Title metadata
          public string Title;

        //     Artist metadata
          public string Artist;

        //     Genre metadata
        public string Genre;

        //     Copyright metadata
        public string Copyright;

        //     Album metadata
        public string Album;

        //     Track number metadata
        public string TrackNumber;

        //     Description metadata
        public string Description;

        //     Rating metadata
        public string Rating;

        //     Date metadata
        public string Date;

        //     Setting metadata
        public string Setting;

        //     URL metadata
        public string URL;

        //     Language metadata
        public string Language;

        //     Now playing metadata
        public string NowPlaying;

        //     Publisher metadata
        public string Publisher;

        //     Encoded by metadata
        public string EncodedBy;
        //
        // Summary:
        //     Artwork URL metadata
        public string ArtworkURL;

        //     Track ID metadata
        public string TrackID;

        //     Total track metadata
        public string TrackTotal;

        //     Director metadata
        public string Director;

        //     Season metadata
        public string Season;

        //     Episode metadata
        public string Episode;

        //     Show name metadata
        public string ShowName,

            //     Actors metadata
            public string Actors;

        //     Album artist metadata
        public string AlbumArtist;

        //     Disc number metadata
        public string DiscNumber;

        //     Disc total metadata
        public string DiscTotal;
    


        public void Fill(string path)
        {
            var media = new LibVLCSharp.Shared.Media(LibVLC, path);

            Title = media.Meta(MetadataType.Title);

            Artist = media.Meta(MetadataType.Artist);

            Genre = media.Meta(MetadataType.Genre);

            Copyright = media.Meta(MetadataType.Copyright);

            Album = media.Meta(MetadataType.Album);

            TrackNumber = media.Meta(MetadataType.TrackNumber);

            Description = media.Meta(MetadataType.Description);

            Rating = media.Meta(MetadataType.Rating);

            Date = media.Meta(MetadataType.Date);

            Setting = media.Meta(MetadataType.Setting);

            URL = media.Meta(MetadataType.URL);

            Language = media.Meta(MetadataType.Language);,

            NowPlaying = media.Meta(MetadataType.NowPlaying);

             Publisher = media.Meta(MetadataType.Publisher);

            EncodedBy = media.Meta(MetadataType.EncodedBy);

            ArtworkURL = media.Meta(MetadataType.ArtworkURL);

            TrackID = media.Meta(MetadataType.TrackID);

            TrackTotal = media.Meta(MetadataType.TrackTotal);

            Director = media.Meta(MetadataType.Director);

           Season = media.Meta(MetadataType.Season);

            Episode = media.Meta(MetadataType.Episode);

            ShowName = media.Meta(MetadataType.ShowName);

            Actors = media.Meta(MetadataType.Actors);

            AlbumArtist = media.Meta(MetadataType.AlbumArtist);

            DiscNumber = media.Meta(MetadataType.DiscNumber);

            DiscTotal = media.Meta(MetadataType.DiscTotal);
        }


    }
}
