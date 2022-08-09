using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace VidFileTag.Model
{
    [Table("TagSetInfo")]
    public partial class TagSetInfo
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(40, ErrorMessage = "The TagSet length cannot exceed 40 characters. ")]
        public string TagSet { get; set; } = string.Empty;

        [MaxLength(2400, ErrorMessage = "The TagDecription length cannot exceed 2400 characters. ")]
        public string TagSetDecription { get; set; } = string.Empty;

        public virtual ICollection<TagInfoTagSetInfo> TagInfos { get; set; } = new HashSet<TagInfoTagSetInfo>();
    }
}
