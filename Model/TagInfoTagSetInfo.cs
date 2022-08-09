using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace VidFileTag.Model
{
    [Table("TagInfoTagSetInfo")]
    public partial class TagInfoTagSetInfo
    {
        private string shown = string.Empty;
        public int TagInfoId { get; set; }
        public TagInfo TagInfo { get; set; }
        public int TagSetInfoId { get; set; }
        public TagSetInfo TagSetInfo { get; set; }


        [NotMapped]
        public string Shown
        {
            get
            {
                if (shown == null)
                {
                    if (TagInfo != null && TagSetInfo != null)
                    {
                        shown = $"{TagSetInfo.TagSet} <-> {TagInfo.Tag}";
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
