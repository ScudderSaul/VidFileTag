using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace VidFileTag.Model
{
    [Table("TagFileInfo")]
    public partial class TagFileInfo
    {

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; } = string.Empty;

        public string FileExtension { get; set; } = string.Empty;


        [Required]
        public string FilePath { get; set; } = string.Empty;

        public long FileSize { get; set; } = 0L;

        public uint Crc32 { get; set; } = 0;

        public string FrameHeight { get; set; } = string.Empty;

        public string FrameWidth { get; set; } = string.Empty;

        public virtual ICollection<TagFileInfoTagInfo> TagInfos { get; set; } = new HashSet<TagFileInfoTagInfo>();

    }



}
