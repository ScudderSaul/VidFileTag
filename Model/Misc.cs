using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace VidFileTag.Model
{
    [Table("MiscInfo")]
    public partial class MiscInfo
    {

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string MachineName { get; set; } = string.Empty;

        public string LastDiskUsed { get; set; } = @"c:\";

        public string LastPathUsed { get; set; } = string.Empty;

        public string PlayListPath { get; set; } = string.Empty;
    }
}
