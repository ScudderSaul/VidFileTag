using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace VidFileTag.Model
{
    [Table("TagInfo")]
    public partial class TagInfo
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(40, ErrorMessage = "The Tag length cannot exceed 40 characters. ")]
        public string Tag { get; set; } = string.Empty;

        [MaxLength(2400, ErrorMessage = "The TagDecription length cannot exceed 2400 characters. ")]
        public string TagDecription { get; set; } = string.Empty;

        public virtual ICollection<TagFileInfoTagInfo> TagFileInfos { get; set; } = new HashSet<TagFileInfoTagInfo>();

        public virtual ICollection<TagInfoTagSetInfo> TagSetInfos { get; set; } = new HashSet<TagInfoTagSetInfo>();
    }
}
