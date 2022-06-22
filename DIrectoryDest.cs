using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;



namespace VidFileTag
{
    public partial class DIrectoryDest
    {
        public string Path { get; set; }
        public bool Exits()
        {
            if(System.IO.Directory.Exists(Path))
            {
                return (true);
            }
            return false;
        }

        public void Find()
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Time to select a folder",
                UseDescriptionForTitle = true,
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)  + "\\",  ShowNewFolderButton = true    };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Path = dialog.SelectedPath;
            }
            else
            {
                Path = string.Empty;
            }
        }

    }
}
