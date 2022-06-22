using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace VidFileTag.Model
{
    [Table("TagFileInfoTagInfo")]
    public partial class TagFileInfoTagInfo
    {
        private string shown = string.Empty;
        public int TagFileInfoId { get; set; }
        public TagFileInfo TagFileInfo { get; set; } 

        public int TagInfoId { get; set; }

        public TagInfo TagInfo { get; set; }


        [NotMapped]
        public string Shown
        {
            get
            {
                if (shown == null)
                {
                    if (TagInfo != null && TagFileInfo != null)
                    {
                        shown = $"{TagFileInfo.FileName} <-> {TagInfo.Tag}";
                        return shown;
                    }

                }
                else
                {
                    shown = null;
                }
                return shown;
            }

        }
    }
}
