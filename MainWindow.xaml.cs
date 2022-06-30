using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VidFileTag.Model;
using LibVLCSharp.WPF;
using LibVLCSharp.Shared;
using Soft160.Data.Cryptography;
using System.ComponentModel;
using System.Windows.Forms;

namespace VidFileTag
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string path = string.Empty;
        List<string> dirs = new();
        List<string> filenames;
        Dictionary<string, string> _VLCfileformats = null;

        Random rand = new();

        SortedList<string, TagInfo> SortedTags = new();
        private Dictionary<string, string> mediaInfo;

        //    List<TagFileInfoTagInfo> fileTags = new List<TagFileInfoTagInfo>();


        private LibVLC _libVLC;
        private LibVLCSharp.Shared.MediaPlayer _mediaPlayer;


        //  DirectoryInfo vlcLibDirectory;
        private static TagModel _context;
        public static string ConnectionOther = string.Empty;

        private TagFileInfo _selectedFileInfo = null;
        private bool _preExistingSelected = false;

        #region ctor
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            DriveInfos(); // disks listed
            BuildVlcTypesDictionary();  // media types reference list

            Core.Initialize();


            Back.Content = "\u21E1";
            //    ShowFileButton.Content = "Show \u25b6";
            //     PauseFileButton.Content = "Pause \u23F8";
            //    StopFileButton.Content = "Stop \u23F9";

            _libVLC = new LibVLC();    // lib vlc

            LoadMachineData();  // restore last drive path
            ReadTags();         // read tags from database

        }


        // define an event handler
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion


        #region Properties

        public string SelectedCopyDestination { get; set; } = string.Empty;

        public Dictionary<string, string> MediaInfo
        {
            get => mediaInfo;
            private set => Set(nameof(MediaInfo), ref mediaInfo, value);
        }


        // sqlite dc context property
        public static TagModel Context
        {
            get
            {
                if (_context == null)
                {
                    try
                    {

                        _context = new TagModel();
                        _context.Database.EnsureCreated();

                    }
                    catch (Exception e)
                    {


                        System.Windows.MessageBox.Show(e.Message, "No DataBase Found", MessageBoxButton.OK);

                        throw;
                    }
                }
                return _context;
            }
        }

        //    public List<TagFileInfoTagInfo> FileTags { get => fileTags; set => fileTags = value; }

        // info for the current selected file
        public TagFileInfo SelectedFileInfo { get => _selectedFileInfo; set => _selectedFileInfo = value; }


        public string Seldir { get; set; }

        public bool SetupOn { get; set; } = false;


        // LibVLC property
        public LibVLC LibVLC
        {
            get => _libVLC;
            private set => Set(nameof(LibVLC), ref _libVLC, value);
        }



        // TakeSnapshot(uint num, string? filePath, uint width, uint height);

        /// <summary>
        /// Gets the <see cref="LibVLCSharp.Shared.MediaPlayer"/> instance.
        /// </summary>
        public LibVLCSharp.Shared.MediaPlayer MediaPlayer
        {
            get => _mediaPlayer;
            private set => Set(nameof(MediaPlayer), ref _mediaPlayer, value);
        }


        #endregion


        // add machine data to db if it is not already there.
        public void LoadMachineData()
        {
            string mn = Environment.MachineName;
            string apath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            var mdat = from vv in Context.MiscInfos
                       where vv.MachineName == mn
                       select vv;


            if (mdat.Any())
            {
                MachineTextBlock.Text = mdat.First().MachineName;
                DriveFromRootPath(mdat.First().LastDiskUsed);
                path = mdat.First().LastPathUsed;
            }
            else
            {
                MiscInfo minf = new()
                {
                    MachineName = Environment.MachineName,
                    LastPathUsed = apath,
                    LastDiskUsed = System.IO.Path.GetPathRoot(apath),
                };
                Context.MiscInfos.Add(minf);
                Context.SaveChanges();
                DriveFromRootPath(minf.LastDiskUsed);
                path = apath;
            }
            PathCalks(path);
            LoadDestinationPaths();
        }

        public void UpdateMachineData()
        {
            var mdat = from vv in Context.MiscInfos
                       where vv.MachineName == Environment.MachineName
                       select vv;

            if (mdat.Any())
            {
                MiscInfo minf = mdat.First();
                minf.LastPathUsed = path;
                minf.LastDiskUsed = System.IO.Path.GetPathRoot(path);
                Context.Update(minf);
                Context.SaveChanges();
            }
        }

        public void DriveFromRootPath(string rpath)
        {
            for (int ii = 0; ii < DriveBox.Items.Count; ii++)
            {
                string dbi = (DriveBox.Items[ii] as DriveInfo).RootDirectory.ToString();
                if (dbi == rpath)
                {
                    DriveBox.SelectedIndex = ii;
                    break;
                }
            }
        }

        private void DriveInfos()
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            foreach (DriveInfo d in allDrives)
            {
                DriveBox.Items.Add(d);
            }
        }

        private void GoToParent(object sender, RoutedEventArgs e)
        {
            System.IO.DirectoryInfo pinf = Directory.GetParent(path);

            if (pinf != null)
            {
                PathCalks(pinf.FullName);
                UpdateMachineData();
            }

        }


        // find files when the user selects a child of the current directory 
        private void DirsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.ListView lst)
            {
                if (lst.SelectedIndex >= 0)
                {
                    string nx = lst.SelectedItem as string;

                    PathCalks(nx);
                    UpdateMachineData();
                }
            }
        }


        // change disk drive
        private void DriveBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DriveBox.SelectedIndex >= 0)
            {
                path = (DriveBox.SelectedItem as DriveInfo).Name;
                PathCalks(path);
            }
        }


        // read all tags from the database
        public void ReadTags()
        {
            TagsListView.Items.Clear();
            SortedTags.Clear();

            var ff = from su in Context.TagInfos
                     select su;
            if (ff.Any())
            {
                foreach (TagInfo tt in ff)
                {
                    SortedTags.Add(tt.Tag, tt);
                }

                foreach (KeyValuePair<string, TagInfo> pr in SortedTags)
                {


                    TagsListView.Items.Add(pr.Value);
                }
            }
        }

        #region File Sets


        // Find all files and sub directories under the curent path
        public void PathCalks(string npath)
        {
            try
            {
                string pr = System.IO.Path.GetPathRoot(npath);
                DriveFromRootPath(pr);
                path = npath;


                dirs = new List<string>(Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly));

                filenames = new List<string>(Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly));

                PathTextBlock.Text = path;

                DirsListView.Items.Clear();
                foreach (string dt in dirs)
                {
                    //  string? diname = System.IO.Path.GetDirectoryName(dt);
                    DirsListView.Items.Add(dt);
                }

                FilesListView.Items.Clear();
                foreach (string st in filenames)
                {
                    string finame = System.IO.Path.GetFileName(st);

                    if (string.IsNullOrWhiteSpace(finame))
                    {
                        continue;
                    }

                    // if a file's info is already in tyhe database read it from there abd add it to the file list view
                    TagFileInfo fti = null;
                    var ff = from su in Context.TagFileInfos
                             where su.FilePath == st
                             select su;
                    if (ff.Any())
                    {
                        fti = ff.First();
                        FilesListView.Items.Add(fti);
                    }
                    else  // when it is not in the database create an info class
                    {
                        //  FileInfo fff = new FileInfo(st);

                        fti = new TagFileInfo
                        {
                            FilePath = st,
                            FileName = finame
                        };
                        //    fti.FileSize = fff.Length;
                        //   fti.FileExtension = fff.Extension;
                        FilesListView.Items.Add(fti);
                    }
                }
            }
            catch (Exception ee)
            {
                System.Windows.MessageBox.Show("Path error -- The system message was " + ee.Message);
            }
        }

        private void FilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Windows.Controls.ListView lst = sender as System.Windows.Controls.ListView;
            if (lst != null)
            {
                if (lst.SelectedIndex >= 0)
                {
                    TagsExistView.Items.Clear();
                    TagFileInfo nx = lst.SelectedItem as TagFileInfo;
                    SelectedFile.Text = nx.FileName;

                    var ff = from su in MainWindow.Context.TagFileInfos
                             where su.FilePath == nx.FilePath
                             select su;
                    if (ff.Any())
                    {
                        SelectedFileInfo = ff.First();

                        if (SelectedFileInfo.Crc32 == 0)
                        {
                            FileInfo fi1 = new(nx.FilePath);

                            SelectedFileInfo.FileSize = fi1.Length;
                            SelectedFileInfo.FileExtension = fi1.Extension;
                            SelectedFileInfo.Crc32 = CalculateCRC(fi1);

                            Context.TagFileInfos.Update(SelectedFileInfo);
                            Context.SaveChanges();
                        }

                        _preExistingSelected = true;

                        if (SelectedFileInfo.FrameHeight == string.Empty)
                        {
                            GetFileDetails();
                        }

                        var qq = from ss in Context.TagFileInfoTagInfos
                                 where ss.TagFileInfoId == SelectedFileInfo.Id
                                 select ss;


                        if (qq.Any())
                        {
                            foreach (TagFileInfoTagInfo qqe in qq)
                            {
                                TagInfo ton = qqe.TagInfo;
                                TagsExistView.Items.Add(ton);

                            }
                        }
                    }
                    else
                    {
                        _preExistingSelected = false;
                        SelectedFileInfo = nx;
                        if (SelectedFileInfo.Crc32 == 0)
                        {
                            FileInfo fi1 = new(nx.FilePath);

                            SelectedFileInfo.FileSize = fi1.Length;
                            SelectedFileInfo.FileExtension = fi1.Extension;
                            SelectedFileInfo.Crc32 = CalculateCRC(fi1);
                        }


                        //  FileXtraDetailsGet();
                    }

                    if (_VLCfileformats.Keys.Contains<string>(SelectedFileInfo.FileExtension.ToUpper()) == true)
                    {
                        Cntrl.FilePath = SelectedFileInfo.FilePath;
                        Cntrl.Show();
                        FillMediaInfo(SelectedFileInfo.FilePath);
                    }
                    else
                    {
                        Cntrl.Hide();
                    }

                    //  ShowFileButton.IsEnabled = true;
                    //  PauseFileButton.IsEnabled = true;
                    //   StopFileButton.IsEnabled = true;
                    // TakeCheckSum.IsEnabled = true;
                    // CompareCheckSum.IsEnabled = true;
                }
                else
                {

                    Cntrl.Hide();
                    //    ShowFileButton.IsEnabled = false;
                    //     PauseFileButton.IsEnabled = false;
                    //    StopFileButton.IsEnabled = false;
                    // TakeCheckSum.IsEnabled = false;
                    //  CompareCheckSum.IsEnabled = false;
                }
            }
        }

        public static string FilePathToFileUrl(string filePath)
        {
            StringBuilder uri = new();
            foreach (char v in filePath)
            {
                if ((v >= 'a' && v <= 'z') || (v >= 'A' && v <= 'Z') || (v >= '0' && v <= '9') ||
                  v == '+' || v == '/' || v == ':' || v == '.' || v == '-' || v == '_' || v == '~' ||
                  v > '\xFF')
                {
                    uri.Append(v);
                }
                else if (v == System.IO.Path.DirectorySeparatorChar || v == System.IO.Path.AltDirectorySeparatorChar)
                {
                    uri.Append('/');
                }
                else
                {
                    uri.Append(String.Format("%{0:X2}", (int)v));
                }
            }
            if (uri.Length >= 2 && uri[0] == '/' && uri[1] == '/') // UNC path
                uri.Insert(0, "file:");
            else
                uri.Insert(0, "file:///");
            return uri.ToString();
        }

        // Show the files matching a wild card
        private void WildFileButton_Click(object sender, RoutedEventArgs e)
        {


            string pattern = WildFileTextBox.Text;
            if (pattern == string.Empty)
            {
                PathCalks(path);
                return;
            }
            if (FilesListView.Items.Count == 0)
            {
                PathCalks(path);
                return;
            }
            Cursor = System.Windows.Input.Cursors.Wait;

            try
            {

                filenames = new List<string>(Directory.EnumerateFiles(path, pattern, SearchOption.TopDirectoryOnly));

                FilesListView.Items.Clear();
                foreach (string st in filenames)
                {

                    string finame = System.IO.Path.GetFileName(st);
                    if (string.IsNullOrWhiteSpace(finame))
                    {
                        continue;
                    }

                    // if a file's info is already in tyhe database read it from there abd add it to the file list view
                    TagFileInfo fti = null;
                    var ff = from su in Context.TagFileInfos
                             where su.FilePath == st
                             select su;
                    if (ff.Any())
                    {
                        fti = ff.First();
                        FilesListView.Items.Add(fti);
                    }
                    else  // when it is not in the database create an info class
                    {
                        fti = new TagFileInfo();
                        fti.FilePath = st;
                        fti.FileName = finame;
                        FilesListView.Items.Add(fti);
                    }
                }
            }
            catch (Exception ee)
            {
                System.Windows.MessageBox.Show("Path error -- The system message was " + ee.Message);
                PathCalks(path);
            }
            InvalidateVisual();
            Cursor = System.Windows.Input.Cursors.Arrow;
        }

        // if a tag is selected and a file with that tag is in the path show it in the list
        private void FilesWithASelectedTagInPath_Click(object sender, RoutedEventArgs e)
        {
            var ff = from TagInfo su in TagsListView.SelectedItems
                     select su;
            if (ff.Any() == false)
            {
                return;
            }

            PathCalks(path);

            filenames = new List<string>(Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly));

            var dd = from vv in Context.TagFileInfos
                     where filenames.Contains(vv.FilePath)
                     select vv;

            List<TagFileInfo> mlist = new();

            FilesListView.Items.Clear();

            if (dd.Any())
            {
                Cursor = System.Windows.Input.Cursors.Wait;
                foreach (TagFileInfo oo in dd)
                {
                    foreach (TagInfo tif in ff)
                    {
                        var uu = from ss in Context.TagFileInfoTagInfos
                                 where ss.TagInfoId == tif.Id &&
                                 ss.TagFileInfoId == oo.Id
                                 select ss;

                        if (uu.Any())
                        {
                            if (FilesListView.Items.Contains(oo) == false)
                            {
                                FilesListView.Items.Add(oo);
                            }
                        }
                    }
                }
            }
            InvalidateVisual();
            Cursor = System.Windows.Input.Cursors.Arrow;
        }


        #endregion

        #region add Tags to files

        /// <summary>
        /// A tag is selected or unselected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TagsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            var ff = from TagInfo su in TagsListView.SelectedItems
                     select su;
            if (ff.Any())
            {
                TagInfo tt = ff.First();
                OperationTagTextBox.Text = tt.Tag;
                OperationTagTextBox.Tag = tt;
                if (SelectedFileInfo != null)
                {
                    TagAddButton.IsEnabled = true;
                    TagRemoveButton.IsEnabled = true;
                }

                PlayListButton.IsEnabled = true;
                PlayListAllButton.IsEnabled = true;
                PlayListOneButton.IsEnabled = true;
                FilesWithASelectedTagInPath.IsEnabled = true;
            }
            else
            {
                TagAddButton.IsEnabled = false;
                PlayListButton.IsEnabled = false;
                PlayListAllButton.IsEnabled = false;
                PlayListOneButton.IsEnabled = false;
                TagRemoveButton.IsEnabled = false;
                FilesWithASelectedTagInPath.IsEnabled = false;
            }

            this.InvalidateVisual();
        }


        // add the selected file to the DB if it's not there already.
        private void AddSelectedFile()
        {
            var ff = from su in Context.TagFileInfos
                     where su.FilePath == SelectedFileInfo.FilePath
                     select su;

            // add crc to new and old 
            if (ff.Any())
            {
                SelectedFileInfo = ff.First();
                SelectedFileInfo.Crc32 = CalculateCRC(SelectedFileInfo);
                Context.TagFileInfos.Update(SelectedFileInfo);
                Context.SaveChanges();
                return;
            }

            FileInfo fi1 = new(SelectedFileInfo.FilePath);

            SelectedFileInfo.FileSize = fi1.Length;
            SelectedFileInfo.FileExtension = fi1.Extension;
            SelectedFileInfo.Crc32 = CalculateCRC(fi1);

            //   ShellObject mmd = ShellObject.FromParsingName(SelectedFileInfo.FilePath);
            //           SelectedFileInfo.FrameHeight = GetValue(mmd.Properties.GetProperty(SystemProperties.System.Video.FrameHeight));
            //            SelectedFileInfo.FrameWidth = GetValue(mmd.Properties.GetProperty(SystemProperties.System.Video.FrameWidth));

            Context.TagFileInfos.Add(SelectedFileInfo);
            Context.SaveChanges();

            // reload it
            var gg = from su in Context.TagFileInfos
                     where su.FilePath == SelectedFileInfo.FilePath
                     select su;

            if (gg.Any())
            {
                SelectedFileInfo = gg.First();
            }
        }

        // add the selected tags to the selected file
        private void TagAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFileInfo == null)
            {
                return;
            }
            AddSelectedFile();  // first make sure the selected file is in the db

            var ff = from TagInfo su in TagsListView.SelectedItems
                     select su;

            foreach (TagInfo ti in ff)
            {
                // does the file have this tag
                var qq = from tftf in Context.TagFileInfoTagInfos
                         where tftf.TagInfoId == ti.Id &&
                         tftf.TagFileInfoId == SelectedFileInfo.Id
                         select tftf;

                // if not add it
                if (!qq.Any())
                {
                    var rr = new TagFileInfoTagInfo
                    {
                        TagFileInfoId = SelectedFileInfo.Id,
                        TagInfoId = ti.Id,
                    };

                    Context.TagFileInfoTagInfos.Add(rr);
                    Context.SaveChanges();


                    TagsExistView.Items.Add(ti);
                    TagsExistView.InvalidateArrange();
                }
            }
        }


        // remove the selected tags from the selected file
        private void TagRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFileInfo == null)
            {
                return;
            }

            var ff = from TagInfo su in TagsListView.SelectedItems
                     select su;

            if (ff.Any())
            {
                foreach (TagInfo inf in ff)
                {
                    var gg = from TagFileInfoTagInfo bb in Context.TagFileInfoTagInfos
                             where inf.Id == bb.TagInfoId &&
                             SelectedFileInfo.Id == bb.TagFileInfoId
                             select bb;

                    if (gg.Any())
                    {
                        Context.TagFileInfoTagInfos.Remove(gg.First());
                        Context.SaveChanges();

                    }
                }
            }
            TagsExistView.Items.Clear();

            foreach (TagFileInfoTagInfo qqe in SelectedFileInfo.TagInfos)
            {
                TagsExistView.Items.Add(qqe.TagInfo);
            }

            TagsExistView.InvalidateVisual();
        }

        private void AddMediaFilesFromPath()
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Cursor = System.Windows.Input.Cursors.Wait;

            var allfiles = Directory.EnumerateFiles(path);


            foreach (string ss in allfiles)
            {
                FileInfo fiinf = new FileInfo(ss);

                if (_VLCfileformats.Keys.Contains<string>(fiinf.Extension.ToUpper()) == true)
                {
                    var ff = from su in MainWindow.Context.TagFileInfos
                             where su.FilePath == fiinf.FullName
                             select su;
                    if (ff.Any() == false)
                    {
                        TagFileInfo tfinf = new TagFileInfo
                        {
                            FileExtension = fiinf.Extension,
                            FileSize = fiinf.Length,
                            FilePath = fiinf.FullName,
                            FileName = fiinf.Name,


                        };

                        tfinf.Crc32 = CalculateCRC(tfinf);
                        Context.TagFileInfos.Add(tfinf);
                        Context.SaveChanges();
                    }
                }
            }
            Cursor = System.Windows.Input.Cursors.Arrow;
        }



        private void AddMediaFiles_Click(object sender, RoutedEventArgs e)
        {
            AddMediaFilesFromPath();
        }

        bool IsMediaFile(string pathfile)
        {
            FileInfo fiinf = new FileInfo(pathfile);

            if (_VLCfileformats.Keys.Contains<string>(fiinf.Extension.ToUpper()) == true)
            {
                return (true);
            }

            return (false);
        }


        private void TagMediaFilesFromPath()
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var allfiles = Directory.EnumerateFiles(path);

            Cursor = System.Windows.Input.Cursors.Wait;

            var ee = from TagInfo su in TagsListView.SelectedItems
                     select su;

            if (ee.Any())
            {
                foreach (string pn in allfiles)
                {
                    FileInfo fiinf = new FileInfo(pn);

                    if (_VLCfileformats.Keys.Contains<string>(fiinf.Extension.ToUpper()) == true)
                    {
                        var ff = from su in MainWindow.Context.TagFileInfos
                                 where su.FilePath == fiinf.FullName
                                 select su;
                        if (ff.Any() == false)
                        {
                            TagFileInfo tfinf = new TagFileInfo
                            {
                                FileExtension = fiinf.Extension,
                                FileSize = fiinf.Length,
                                FilePath = fiinf.FullName,
                                FileName = fiinf.Name,


                            };
                            tfinf.Crc32 = CalculateCRC(tfinf);
                            Context.TagFileInfos.Add(tfinf);
                            Context.SaveChanges();
                        }
                        var tt = from qq in Context.TagFileInfos
                                 where qq.FilePath == fiinf.FullName
                                 select qq;

                        if (tt.Any())
                        {
                            TagFileInfo oo = tt.First();
                            foreach (TagInfo yy in ee)
                            {
                                var qw = from looko in Context.TagFileInfoTagInfos
                                         where looko.TagInfoId == yy.Id
                                         && looko.TagFileInfoId == oo.Id
                                         select looko;

                                if (!qw.Any())
                                {
                                    TagFileInfoTagInfo tb = new TagFileInfoTagInfo
                                    {
                                        TagFileInfo = oo,
                                        TagFileInfoId = oo.Id,
                                        TagInfo = yy,
                                        TagInfoId = yy.Id
                                    };
                                    Context.TagFileInfoTagInfos.Add(tb);
                                    Context.SaveChanges();
                                }
                            }
                        }
                    }
                }
            }
            Cursor = System.Windows.Input.Cursors.Arrow;
        }

        private void TagMediaFiles_Click(object sender, RoutedEventArgs e)
        {
            TagMediaFilesFromPath();
        }

        #endregion

        #region CRUD tag

        private void TagsExistView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            this.InvalidateVisual();
        }

        // create a tag
        private void CreateTagButton_Click(object sender, RoutedEventArgs e)
        {
            string ts = CreatedTagTextBox.Text;
            if (string.IsNullOrWhiteSpace(ts))
            {
                return;
            }
            var ff = from su in MainWindow.Context.TagInfos
                     where su.Tag == ts
                     select su;
            if (ff.Any())
            {
                return;
            }

            TagInfo ti = new TagInfo();
            ti.Tag = ts;
            MainWindow.Context.TagInfos.Add(ti);
            MainWindow.Context.SaveChanges();
            ReadTags();
        }

        // update a tag
        private void UpdateTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (OperationTagTextBox.Tag != null && string.IsNullOrWhiteSpace(OperationTagTextBox.Text) == false)
            {
                var ff = from su in MainWindow.Context.TagInfos
                         where su.Tag == OperationTagTextBox.Text
                         select su;
                if (ff.Any())
                {
                    System.Windows.MessageBox.Show("Modification already exists as Tag");
                    return;
                }
                TagInfo inf = (OperationTagTextBox.Tag as TagInfo);
                if (inf != null)
                {

                    inf.Tag = OperationTagTextBox.Text;
                    Context.TagInfos.Update(inf);
                    Context.SaveChanges();
                    ReadTags();
                }
            }
        }

        // delete a tag and its uses.
        private void DeleteTagButton_Click(object sender, RoutedEventArgs e)
        {
            TagInfo inf = (OperationTagTextBox.Tag as TagInfo);
            if (inf != null)
            {
                var yui = from oio in Context.TagFileInfoTagInfos
                          where oio.TagInfoId == inf.Id
                          select oio;
                if (yui.Any())
                {
                    foreach (TagFileInfoTagInfo ity in yui)
                    {
                        Context.TagFileInfoTagInfos.Remove(ity);
                    }
                }
                Context.SaveChanges();

                Context.TagInfos.Remove(inf);
                Context.SaveChanges();

                OperationTagTextBox.Tag = null;
                OperationTagTextBox.Text = string.Empty;
                ReadTags();
            }
        }

        #endregion

        #region playlists
        private void PlayListButton_Click(object sender, RoutedEventArgs e)
        {
            Random rand = new Random();
            List<TagFileInfo> FileList = new List<TagFileInfo>();
            var ff = from TagInfo su in TagsListView.SelectedItems
                     select su;


            if (ff.Any())
            {
                string filebuf = string.Empty;

                foreach (TagInfo ti in ff)
                {
                    var tt = from TagFileInfoTagInfo ln in Context.TagFileInfoTagInfos
                             where ln.TagInfoId == ti.Id
                             select ln;

                    if (tt.Any())
                    {
                        foreach (TagFileInfoTagInfo ln in tt)
                        {
                            var gu = from fil in Context.TagFileInfos
                                     where fil.Id == ln.TagFileInfoId
                                     select fil;
                            if (gu.Any())
                            {
                                TagFileInfo tfi = gu.First();
                                if (FileList.Contains(tfi) == false)
                                {
                                    FileList.Add(tfi);
                                    filebuf += $"{tfi.FilePath}" + " \r\n";
                                }

                            }

                        }
                    }

                }

                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = $"Playlist_{ff.First().Tag}Plus{rand.Next() % 1999}"; // Default file name
                dlg.DefaultExt = ".VLC"; // Default file extension
                dlg.Filter = "VLC documents (.vlc)|*.vlc"; // Filter files by extension

                Nullable<bool> result = dlg.ShowDialog();

                if (result == true)
                {
                    Stream st = dlg.OpenFile();
                    using (StreamWriter sw = new StreamWriter(st))
                    {
                        sw.WriteLine(filebuf);
                    }
                }
            }
        }

        private void PlayListAllButton_Click(object sender, RoutedEventArgs e)
        {
            Random rand = new Random();
            List<TagFileInfo> FileList = new List<TagFileInfo>();
            List<TagFileInfo> RemoveList = new List<TagFileInfo>();
            List<TagInfo> TagList = new List<TagInfo>();
            string filebuf = string.Empty;

            // get selected tags
            var ff = from TagInfo su in TagsListView.SelectedItems
                     select su;
            Cursor = System.Windows.Input.Cursors.Wait;
            if (ff.Any())
            {


                foreach (TagInfo ti in ff)
                {
                    // read each tag from db put in list
                    var qq = from tftf in Context.TagInfos
                             where tftf.Id == ti.Id
                             select tftf;
                    if (qq.Any())
                    {
                        TagList.Add(qq.First());
                    }
                }
            }
            else
            {
                return;
            }

            foreach (TagInfo ti in TagList)
            {
                // for each tag add all files info using it to a list if they are not there

                var gu = from fil in ti.TagFileInfos
                         select fil;

                foreach (TagFileInfoTagInfo ee in gu)
                {
                    if (FileList.Contains(ee.TagFileInfo) == false)
                    {
                        FileList.Add(ee.TagFileInfo);
                    }
                }
            }

            foreach (TagInfo ti in TagList)
            {
                foreach (TagFileInfo tfi in FileList)
                {
                    var inf = from yy in Context.TagFileInfoTagInfos
                              where yy.TagFileInfo.Id == tfi.Id &&
                              yy.TagInfoId == ti.Id
                              select yy;

                    if (inf.Any() == false)
                    {
                        if (RemoveList.Contains(tfi) == false)
                        {
                            RemoveList.Add(tfi);
                        }
                    }
                }
            }

            foreach (TagFileInfo tagfi in RemoveList)
            {
                if (FileList.Contains(tagfi))
                {
                    FileList.Remove(tagfi);
                }
            }

            int cnt = 0;

            foreach (TagFileInfo tagfi in FileList)
            {
                filebuf += $"{tagfi.FilePath}" + " \r\n";
                cnt++;
            }

            if (cnt == 0)
            {
                Cursor = System.Windows.Input.Cursors.Arrow;
                System.Windows.MessageBox.Show("No files had all of the selected tags");
                return;
            }
            Cursor = System.Windows.Input.Cursors.Arrow;

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = $"Playlist_{ff.First().Tag}Plus{rand.Next() % 1999}"; // Default file name
            dlg.DefaultExt = ".VLC"; // Default file extension
            dlg.Filter = "VLC documents (.vlc)|*.vlc"; // Filter files by extension

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                Stream st = dlg.OpenFile();
                using (StreamWriter sw = new StreamWriter(st))
                {
                    sw.WriteLine(filebuf);

                }
            }

        }

        private void PlayListOneButton_Click(object sender, RoutedEventArgs e)
        {
            Cursor = System.Windows.Input.Cursors.Wait;
            List<TagFileInfo> FileList = new List<TagFileInfo>();
            List<TagInfo> TagList = new List<TagInfo>();
            var ff = from TagInfo su in TagsListView.SelectedItems
                     select su;

            string filebuf = string.Empty;
            if (ff.Any())
            {
                TagList = ff.ToList();

                var hh = from TagFileInfoTagInfo hhh in Context.TagFileInfoTagInfos
                         where TagList.Contains(hhh.TagInfo)
                         select hhh;

                if (hh.Any())
                {
                    List<TagFileInfoTagInfo> iinn = hh.ToList();

                    var fi = from TagFileInfoTagInfo uh in iinn
                             select uh.TagFileInfo;

                    foreach (TagFileInfo rtr in fi)
                    {
                        int cnt = 0;

                        foreach (TagFileInfoTagInfo uh in iinn)
                        {
                            if (rtr.Id == uh.TagFileInfoId)
                            {
                                cnt++;
                            }
                        }

                        if (cnt == 1 && !FileList.Contains(rtr))
                        {
                            FileList.Add(rtr);
                            filebuf += $"{rtr.FilePath}" + " \r\n";
                        }
                    }
                }

                if (FileList.Count == 0)
                {
                    Cursor = System.Windows.Input.Cursors.Arrow;
                    System.Windows.MessageBox.Show("No files had just one selected tags");
                    return;
                }
                Cursor = System.Windows.Input.Cursors.Arrow;

                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = $"Playlist_{ff.First().Tag}Plus{rand.Next() % 1999}"; // Default file name
                dlg.DefaultExt = ".VLC"; // Default file extension
                dlg.Filter = "VLC documents (.vlc)|*.vlc"; // Filter files by extension

                Nullable<bool> result = dlg.ShowDialog();

                if (result == true)
                {
                    Stream st = dlg.OpenFile();
                    using (StreamWriter sw = new StreamWriter(st))
                    {
                        sw.WriteLine(filebuf);
                    }
                }
            }
        }



        #endregion

        #region Play Media files
        public void BuildVlcTypesDictionary()
        {
            _VLCfileformats = new Dictionary<string, string>();

            _VLCfileformats[".3G2"] = "3GPP2 Multimedia File";
            _VLCfileformats[".3GA"] = "3GPP Audio File";
            _VLCfileformats[".3GP"] = "3GPP Multimedia File";
            _VLCfileformats[".AAC"] = "Advanced Audio Coding File";
            _VLCfileformats[".ADT"] = "ADTS Audio File";
            _VLCfileformats[".ANX"] = "Annodex Exchange Format File";
            _VLCfileformats[".AVCHD"] = "High Definition Video File";
            _VLCfileformats[".AVI"] = "Audio Video Interleave File";
            _VLCfileformats[".BIK"] = "Bink Video File";
            _VLCfileformats[".F4V"] = "Flash MP4 Video File";
            _VLCfileformats[".FLV"] = "Flash Video File";
            _VLCfileformats[".GXF"] = "General eXchange Format File";
            _VLCfileformats[".H264"] = "H.264 Encoded Video File";
            _VLCfileformats[".HDMOV"] = "QuickTime HD Movie File";
            _VLCfileformats[".ISO"] = "Disc Image File";
            _VLCfileformats[".M1V"] = "MPEG - 1 Video File";
            _VLCfileformats[".M2V"] = "MPEG - 2 Video";
            _VLCfileformats[".M4A"] = "MPEG - 4 Audio File";
            _VLCfileformats[".M4B"] = "MPEG - 4 Audio Book File";
            _VLCfileformats[".M4V"] = "  iTunes Video File";
            _VLCfileformats[".MKA"] = "Matroska Audio File";
            _VLCfileformats[".MKS"] = "Matroska Elementary Stream File";
            _VLCfileformats[".MOV"] = "Apple QuickTime Movie";
            _VLCfileformats[".MP3"] = "MP3 Audio File";
            _VLCfileformats[".MP4"] = "MPEG - 4 Video File";
            _VLCfileformats[".MPEG1"] = "MPEG - 1 Video File";
            _VLCfileformats[".MPEG4"] = "MPEG - 4 File";
            _VLCfileformats[".MPG2"] = "MPEG - 2 Video File";
            _VLCfileformats[".MPV"] = "MPEG Elementary Stream Video File";
            _VLCfileformats[".MTS"] = "AVCHD Video File";
            _VLCfileformats[".NSV"] = "Nullsoft Streaming Video File";
            _VLCfileformats[".NUV"] = "NuppelVideo File";
            _VLCfileformats[".OGG"] = "Ogg Vorbis Audio File";
            _VLCfileformats[".OGV"] = "Ogg Video File";
            _VLCfileformats[".OMA"] = "Sony OpenMG Music File";
            _VLCfileformats[".OPUS"] = "Opus Audio File";
            _VLCfileformats[".PSS"] = "PlayStation 2 Game Video File";
            _VLCfileformats[".RA"] = "Real Audio File";
            _VLCfileformats[".RBS"] = "MP3 Ringtone File";
            _VLCfileformats[".REC"] = "Topfield PVR Recording";
            _VLCfileformats[".RM"] = "RealMedia File";
            _VLCfileformats[".RMI"] = "RMID MIDI File";
            _VLCfileformats[".RT"] = "RealText Streaming Text File";
            _VLCfileformats[".SPX"] = "Ogg Vorbis Speex File";
            _VLCfileformats[".SVI"] = "Samsung Video File";
            _VLCfileformats[".TOD"] = "JVC Everio Video Capture File";
            _VLCfileformats[".TRP"] = "HD Video Transport Stream";
            _VLCfileformats[".TS"] = "Video Transport Stream File";
            _VLCfileformats[".TTA"] = "True Audio File";
            _VLCfileformats[".VLT"] = "VLC Media Player Skin File";
            _VLCfileformats[".VOC"] = "Creative Labs Audio File";
            _VLCfileformats[".VP6"] = "TrueMotion VP6 Video File";
            _VLCfileformats[".VQF"] = "TwinVQ Audio File";
            _VLCfileformats[".VRO"] = "DVD Video Recording Format";
            _VLCfileformats[".WAV"] = "WAVE Audio File";
            _VLCfileformats[".WEBM"] = "WebM Video File";
            _VLCfileformats[".WV"] = "WavPack Audio File";
            _VLCfileformats[".XESC"] = "Expression Encoder Screen Capture File";
            _VLCfileformats[".ZAB"] = "Zipped Audio Book";
            _VLCfileformats[".PNG"] = "png image";
            _VLCfileformats[".GIF"] = "gif image";
            _VLCfileformats[".JPG"] = "JPEG image";
            _VLCfileformats[".BMP"] = "Bitmap image";
        }

        //   VidWindow vid { get; set; }


        private CntrlWindow cntrlWindow;
        private CntrlWindow Cntrl
        {
            get
            {
                if (cntrlWindow == null)
                {
                    cntrlWindow = new CntrlWindow();
                    cntrlWindow.Closing += Cntrl_Closing;
                }
                return cntrlWindow;
            }


        }

        private void Cntrl_Closing(object sender, CancelEventArgs e)
        {
            cntrlWindow = null;
            e.Cancel = true;
        }



        #endregion

        private void CreatedTagTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CreatedTagTextBox.Text) == false)
            {
                CreateTagButton.IsEnabled = true;
            }
            else
            {
                CreateTagButton.IsEnabled = false;
            }
        }

        private void TakeCheckSum_Click(object sender, RoutedEventArgs e)
        {
            GetFileDetails();
        }


        private bool AddSelectedToOrReadSelectedFromDb()
        {
            if (!System.IO.File.Exists(SelectedFileInfo.FilePath))
            {
                return false;
            }

            var yy = from TagFileInfo hgh in Context.TagFileInfos
                     where hgh.FilePath == SelectedFileInfo.FilePath
                     select hgh;
            if (yy.Any())
            {
                SelectedFileInfo = yy.First();
                SelectedFileInfo.Crc32 = CalculateCRC(SelectedFileInfo);
                if (IsMediaFile(SelectedFileInfo.FilePath) == false)
                {
                    return true;
                }
                //  FileXtraDetailsGet();
                return true;
            }
            else
            {
                return (true);
            }
        }

        private void GetFileDetails()
        {
            if (SelectedFileInfo != null)
            {
                if (!System.IO.File.Exists(SelectedFileInfo.FilePath))
                {
                    return;
                }

                if (IsMediaFile(SelectedFileInfo.FilePath) == false)
                {
                    return;
                }

                var fi = from vv in Context.TagFileInfos
                         where vv.FilePath == SelectedFileInfo.FilePath
                         select vv;

                if (fi.Any())
                {
                    SelectedFileInfo = fi.First();
                    FileInfo fff = new FileInfo(SelectedFileInfo.FilePath);
                    SelectedFileInfo.FileExtension = fff.Extension;
                    SelectedFileInfo.FileSize = fff.Length;
                    SelectedFileInfo.Crc32 = CalculateCRC(SelectedFileInfo);
                }

                // FileXtraDetailsGet();

                Context.TagFileInfos.Update(SelectedFileInfo);
                Context.SaveChanges();
            }
        }

        private void GetFileDetails_Click(object sender, RoutedEventArgs e)
        {
            GetFileDetails();
        }


        private void CopyFilesWithSelectedTags()
        {
            var ff = from TagInfo su in TagsListView.SelectedItems
                     select su;


            // if there are  selected tags
            if (ff.Any())
            {
                // get a folder path

                FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();

                string folderName = string.Empty;
                dlg.RootFolder = Environment.SpecialFolder.Personal;

                System.Windows.Forms.DialogResult result = dlg.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    folderName = dlg.SelectedPath;
                }
                else
                {
                    return;
                }

                // a list forcopied files

                List<TagFileInfo> done = new List<TagFileInfo>();

                foreach (TagInfo tif in ff)
                {
                    var rel = from rrt in Context.TagFileInfoTagInfos
                              where rrt.TagInfoId == tif.Id
                              select rrt;

                    if (rel.Any())
                    {
                        foreach (TagFileInfoTagInfo aa in rel)
                        {

                            try
                            {
                                if (done.Contains(aa.TagFileInfo) == true)
                                {
                                    continue;

                                }
                                string npath = System.IO.Path.Combine(folderName, aa.TagFileInfo.FileName);
                                if (File.Exists(npath) == true)
                                {
                                    continue;
                                }


                                File.Copy(aa.TagFileInfo.FilePath, npath);
                                done.Add(aa.TagFileInfo);
                                TagFileInfo tfin = new TagFileInfo
                                {
                                    FileSize = aa.TagFileInfo.FileSize,
                                    FileExtension = aa.TagFileInfo.FileExtension,
                                    FilePath = npath,
                                    FileName = aa.TagFileInfo.FileName,
                                    Crc32 = aa.TagFileInfo.Crc32,
                                    FrameHeight = aa.TagFileInfo.FrameHeight,
                                    FrameWidth = aa.TagFileInfo.FrameWidth,
                                };

                                Context.TagFileInfos.Add(tfin);
                                Context.SaveChanges();

                                TagFileInfoTagInfo ninf = new TagFileInfoTagInfo
                                {
                                    TagFileInfo = tfin,
                                    TagInfo = tif,
                                };

                                Context.TagFileInfoTagInfos.Add(ninf);
                                Context.SaveChanges();

                            }
                            catch (Exception ee)
                            {
                                string nope = ee.Message;
                            }



                            // Directory.Move(aa.FilePath, System.IO.Path.Combine(folderName, aa.FileName));
                        }
                    }

                }
            }
        }

        /// <summary>
        /// Moce the files with any selected tag to a new location
        /// </summary>
        private void MoveFilesWithSelectedTags()
        {
            var ff = from TagInfo su in TagsListView.SelectedItems
                     select su;


            // if there are  selected tags
            if (ff.Any())
            {
                // get a folder path

                System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();

                string folderName = string.Empty;
                dlg.RootFolder = Environment.SpecialFolder.Personal;

                System.Windows.Forms.DialogResult result = dlg.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    folderName = dlg.SelectedPath;
                }
                else
                {
                    return;
                }

                // a list forcopied files

                List<TagFileInfo> done = new List<TagFileInfo>();

                foreach (TagInfo tif in ff)
                {
                    var rel = from rrt in Context.TagFileInfoTagInfos
                              where rrt.TagInfoId == tif.Id
                              select rrt;

                    if (rel.Any())
                    {
                        foreach (TagFileInfoTagInfo aa in rel)
                        {

                            try
                            {
                                if (done.Contains(aa.TagFileInfo) == true)
                                {
                                    continue;

                                }
                                string npath = System.IO.Path.Combine(folderName, aa.TagFileInfo.FileName);
                                if (File.Exists(npath) == true)
                                {
                                    continue;
                                }


                                File.Copy(aa.TagFileInfo.FilePath, npath);
                                done.Add(aa.TagFileInfo);
                                TagFileInfo tfin = new TagFileInfo
                                {
                                    FileSize = aa.TagFileInfo.FileSize,
                                    FileExtension = aa.TagFileInfo.FileExtension,
                                    FilePath = npath,
                                    FileName = aa.TagFileInfo.FileName,
                                    Crc32 = aa.TagFileInfo.Crc32,
                                    FrameHeight = aa.TagFileInfo.FrameHeight,
                                    FrameWidth = aa.TagFileInfo.FrameWidth,
                                };

                                Context.TagFileInfos.Add(tfin);
                                Context.SaveChanges();

                                TagFileInfoTagInfo ninf = new TagFileInfoTagInfo
                                {
                                    TagFileInfo = tfin,
                                    TagInfo = tif,
                                };

                                Context.TagFileInfoTagInfos.Add(ninf);
                                Context.SaveChanges();

                            }
                            catch (Exception ee)
                            {
                                System.Windows.MessageBox.Show("Copy before move error -- The system message was " + ee.Message);
                            }
                        }
                    }
                }

                foreach (TagFileInfo bye in done)
                {
                    try
                    {
                        DeleteFile(bye);
                    }
                    catch (Exception ee)
                    {
                        System.Windows.MessageBox.Show("delete error -- The system message was " + ee.Message);
                    }

                }
            }
        }

        /// <summary>
        /// Delete the file from TagFileInfo tz
        /// </summary>
        /// <param name="tz"></param>
        /// <returns></returns>
        private bool DeleteFile(TagFileInfo tz)
        {
            if (File.Exists(tz.FilePath) == true)
            {

                FileInfo ff = new FileInfo(tz.FilePath);

                var yyy = from yaya in Context.TagFileInfoTagInfos
                          where yaya.TagFileInfoId == tz.Id
                          select yaya;

                if (yyy.Any())
                {
                    foreach (var ana in yyy)
                    {
                        Context.TagFileInfoTagInfos.Remove(ana);
                    }
                    Context.SaveChanges();

                }

                var mm = from TagFileInfo gg in Context.TagFileInfos
                         where gg.FilePath == tz.FilePath
                         select gg;

                if (mm.Any())
                {
                    Context.TagFileInfos.Remove(mm.First());
                    Context.SaveChanges();
                }

                ff.Delete();

                return true;
            }
            return (false);

        }


        private void AllCheckSum_Click(object sender, RoutedEventArgs e)
        {
            var rr = from vv in Context.TagFileInfos
                     where vv.Crc32 == 0
                     select vv;

            Cursor = System.Windows.Input.Cursors.Wait;

            foreach (TagFileInfo inf in rr)
            {
                try
                {
                    if (inf.Crc32 == 0)
                    {
                        inf.Crc32 = CalculateCRC(inf);
                        Context.TagFileInfos.Update(inf);
                        Context.SaveChanges();
                    }

                }
                catch (Exception ee)
                {
                    string huh = ee.Message;
                }
            }
            Cursor = System.Windows.Input.Cursors.Arrow;
        }


        /// <summary>
        /// Delete TagFileInfo tz if a file does not exist
        /// </summary>
        /// <param name="tz"></param>
        /// <returns></returns>
        private bool DeleteFileEntrieWithoutAFile(TagFileInfo tz)
        {
            if (File.Exists(tz.FilePath) == false)
            {
                var yyy = from yaya in Context.TagFileInfoTagInfos
                          where yaya.TagFileInfoId == tz.Id
                          select yaya;

                if (yyy.Any())
                {
                    foreach (var ana in yyy)
                    {
                        Context.TagFileInfoTagInfos.Remove(ana);
                    }
                    Context.SaveChanges();

                }
                Context.TagFileInfos.Remove(tz);
                Context.SaveChanges();
                return true;
            }
            return (false);
        }

        private void CombineDuplicateFileEntries(TagFileInfo a, TagFileInfo b)
        {

            if (a.FileSize == b.FileSize && a.FilePath != b.FilePath)
            {
                // get b's tag entries
                var ww = from TagFileInfoTagInfo uu in Context.TagFileInfoTagInfos
                         where uu.TagFileInfoId == b.Id
                         select uu;
                if (ww.Any())
                {
                    foreach (TagFileInfoTagInfo uu in ww)
                    {
                        // if a has a tag entriy pointing to the same tag, 
                        var qq = from TagFileInfoTagInfo cc in Context.TagFileInfoTagInfos
                                 where cc.TagFileInfoId == a.Id && cc.TagInfoId == uu.TagInfoId
                                 select cc;
                        if (qq.Any())
                        {
                            Context.TagFileInfoTagInfos.Remove(uu);
                            Context.SaveChanges();
                        }
                        else
                        {
                            TagFileInfoTagInfo gg = new TagFileInfoTagInfo()
                            {
                                TagFileInfo = a,
                                TagFileInfoId = a.Id,
                                TagInfoId = uu.TagInfoId,
                                TagInfo = uu.TagInfo,
                            };
                            Context.TagFileInfoTagInfos.Remove(uu);
                            Context.TagFileInfoTagInfos.Add(gg);
                            Context.SaveChanges();
                        }
                    }
                }
                Context.TagFileInfos.Remove(b);
                Context.SaveChanges();
            }

        }

        #region CRC
        /// <summary>
        /// Calculate file CRC
        /// </summary>
        /// <param name="TahFileInfo">TagFileInfo with path</param>
        /// <returns>File CRC</returns>
        public static uint CalculateCRC(TagFileInfo sinfo)
        {
            if (sinfo == null)
            {
                return (0);
            }

            uint iscrc = 0;
            try
            {
                if (File.Exists(sinfo.FilePath))
                {
                    using (Stream fileStream = new FileStream(sinfo.FilePath, FileMode.Open, FileAccess.Read))
                    {
                        byte[] fileBuffer = new byte[fileStream.Length];
                        fileStream.Read(fileBuffer, 0, (int)fileStream.Length);
                        fileStream.Close();


                        iscrc = Soft160.Data.Cryptography.CRC.Crc32(fileBuffer);
                    }

                    // sinfo.Crc32 = iscrc;

                }
            }
            catch (Exception ee)
            {
                string st = ee.Message;
            }
            return iscrc;
        }

        /// <summary>
        /// Calculate file CRC
        /// </summary>
        /// <param name="FileInfo">The FileInfo with path</param>
        /// <returns>File CRC</returns>
        public static uint CalculateCRC(System.IO.FileInfo info)
        {
            if (info == null)
            {
                return (0);
            }

            uint iscrc = 0;
            try
            {
                if (info.Exists)
                {
                    using (Stream fileStream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read))
                    {
                        byte[] fileBuffer = new byte[fileStream.Length];
                        fileStream.Read(fileBuffer, 0, (int)fileStream.Length);
                        fileStream.Close();


                        iscrc = Soft160.Data.Cryptography.CRC.Crc32(fileBuffer);
                    }

                    // sinfo.Crc32 = iscrc;

                }
            }
            catch (Exception ee)
            {
                string st = ee.Message;
            }
            return iscrc;
        }

        #endregion

        /// <summary>
        /// If the selected file has the same crc as one in the database and a different path
        /// write a file with the information in it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CompareCheckSum_Click(object sender, RoutedEventArgs e)
        {
            var rr = from vv in Context.TagFileInfos
                     where vv.Crc32 != 0 && vv.Crc32 == SelectedFileInfo.Crc32
                     && vv.FilePath != SelectedFileInfo.FilePath
                     select vv;

            string results = string.Empty;

            if (rr.Any())
            {

                results = $"file = {SelectedFileInfo.FilePath} crc={SelectedFileInfo.Crc32} \r\n";
                foreach (TagFileInfo aa in rr)
                {
                    results += $"file = {aa.FilePath} crc={aa.Crc32} \r\n";
                }

                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = $"Checksum_{rand.Next() % 1999}"; // Default file name
                dlg.DefaultExt = ".txt"; // Default file extension
                dlg.Filter = "txt documents (.txt)|*.txt"; // Filter files by extension

                Nullable<bool> result = dlg.ShowDialog();

                if (result == true)
                {
                    Stream st = dlg.OpenFile();
                    using (StreamWriter sw = new StreamWriter(st))
                    {
                        sw.WriteLine(results);
                    }

                }
            }
            else
            {
                System.Windows.MessageBox.Show("No matching checksums in the database.");
                return;
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

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #region About Box

        /// <summary>
        /// About popup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AboutFileTag_Click(object sender, RoutedEventArgs e)
        {
            AboutPopup.IsOpen = true;
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //   Process.Start(((Hyperlink)sender).NavigateUri.ToString());
            }
            catch (Exception ee)
            {
                string not = ee.Message;
            }

        }

        private void Hyperlink1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //   Process.Start(((Hyperlink)sender).NavigateUri.ToString());
            }
            catch (Exception ee)
            {
                string not = ee.Message;
            }

        }

        /// <summary>
        /// close about popup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PopupClose_Click(object sender, RoutedEventArgs e)
        {
            AboutPopup.IsOpen = false;
        }


        #endregion


        private void ResetpathsButton_Click(object sender, RoutedEventArgs e)
        {
            LocationChangePopup.IsOpen = false;
            Cursor = System.Windows.Input.Cursors.Wait;
            InvalidateVisual();
            if (Directory.Exists(SourcePathTextBox.Text) && Directory.Exists(DestinationPathTextBox.Text) && (SourcePathTextBox.Text != DestinationPathTextBox.Text))
            {

                foreach (TagFileInfo inf in Context.TagFileInfos)
                {
                    string ap = inf.FilePath.Substring(0, inf.FilePath.Length - (inf.FileName.Length + 1));
                    if (ap == SourcePathTextBox.Text)
                    {
                        string destfilepath = DestinationPathTextBox.Text + "\\" + inf.FileName;
                        if (File.Exists(destfilepath))
                        {
                            inf.FilePath = DestinationPathTextBox.Text + "\\" + inf.FileName;
                            Context.Update(inf);
                            Context.SaveChanges();
                        }
                    }
                }

            }
            Cursor = System.Windows.Input.Cursors.Arrow;
            InvalidateVisual();
        }



        private void CancelResetButton_Click(object sender, RoutedEventArgs e)
        {
            LocationChangePopup.IsOpen = false;
        }

        private void ChangeFilePaths_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(PathTextBlock.Text) == false)
            {
                SourcePathTextBox.Text = PathTextBlock.Text;
            }
            else
            {
                SourcePathTextBox.Text = Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            }
            LocationChangePopup.IsOpen = true;
        }

        #region movefiles

        public void MoveSelectedFileClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button bb = sender as System.Windows.Controls.Button;
            DestinationstView.SelectedItem = bb;
            if (SetupOn == true)
            {
                RemoveDestButton();
            }
            else
            {
                MoveSelectedFile();
            }
        }

        public void MoveSelectedFile()
        {

            System.Windows.Controls.Button bb = DestinationstView.SelectedItem as System.Windows.Controls.Button;

            Seldir = bb.Content as string;

            if (string.IsNullOrWhiteSpace(SelectedFile.Text))
            {
                return;
            }

            string msg = "copy " + SelectedFile.Text + " to " + Seldir;
            if (MoveCopyCheckBox.IsChecked == true)
            {
                msg = "move " + SelectedFile.Text + " to " + Seldir;
            }

            var res = System.Windows.MessageBox.Show(msg, "move file and update db", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes || res == MessageBoxResult.OK)
            {
                if (SelectedFileInfo == null)
                {
                    return;
                }
                AddSelectedFile();

                // SelectedFileInfo is in the databas


                string npath = System.IO.Path.Combine(Seldir, SelectedFileInfo.FileName);

                try
                {
                    File.Copy(SelectedFileInfo.FilePath, npath);

                    TagFileInfo yt = new TagFileInfo
                    {
                        FileSize = SelectedFileInfo.FileSize,
                        FileExtension = SelectedFileInfo.FileExtension,
                        FilePath = npath,
                        FileName = SelectedFileInfo.FileName,
                        Crc32 = SelectedFileInfo.Crc32,
                        FrameHeight = SelectedFileInfo.FrameHeight,
                        FrameWidth = SelectedFileInfo.FrameWidth,
                    };
                    Context.TagFileInfos.Add(yt);
                    Context.SaveChanges();


                    var ooi = from TagFileInfoTagInfo oo in Context.TagFileInfoTagInfos
                              where oo.TagFileInfoId == SelectedFileInfo.Id
                              select oo;

                    if (ooi.Any())
                    {
                        foreach (TagFileInfoTagInfo cci in ooi)
                        {
                            TagFileInfoTagInfo ninf = new TagFileInfoTagInfo
                            {
                                TagFileInfo = yt,
                                TagInfo = cci.TagInfo,
                            };

                            Context.TagFileInfoTagInfos.Add(ninf);
                            Context.SaveChanges();
                        }
                    }

                    if (MoveCopyCheckBox.IsChecked == true)
                    {
                        DeleteFile(SelectedFileInfo);
                        TagsExistView.Items.Clear();
                    }

                    if (path != null)
                    {
                        PathCalks(path);
                        SelectedFile.Text = string.Empty;
                    }

                }
                catch (IOException copyError)
                {
                    System.Windows.MessageBox.Show(copyError.Message, npath + "move", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ee)
                {
                    System.Windows.MessageBox.Show(ee.Message, npath + "move", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
            return;

        }

        public void AddDestButton_Click(object sender, RoutedEventArgs e)
        {
            SetupOn = true;

            DIrectoryDest dir = new DIrectoryDest();
            dir.Find();

            if (System.IO.Directory.Exists(dir.Path))
            {
                int lj = DestinationstView.Items.Count;
                for (int ii = 0; ii < lj; ii++)
                {
                    if (DestinationstView.Items[ii] as string == dir.Path)
                    {
                        return;
                    }
                }

                var gfg = from MovePath pp in Context.MovePaths
                          where pp.Path == dir.Path
                          select pp;

                if (gfg.Any())
                {
                    SetupOn = false;
                    return;
                }

                string bname = "ZA" + rand.Next().ToString();

                System.Windows.Controls.Button isdir = new System.Windows.Controls.Button
                {
                    Width = 300,
                    Height = 27,
                    Name = bname,
                };
                isdir.Content = dir.Path;
                isdir.Click += MoveSelectedFileClick;

                DestinationstView.Items.Add(isdir);

                MovePath nv = new MovePath
                {
                    Path = dir.Path,
                };

                Context.MovePaths.Add(nv);
                Context.SaveChanges();



                InvalidateArrange();
                InvalidateMeasure();
                InvalidateVisual();


            }

            SetupOn = false;
            MoveCopyCheckBox.IsChecked = false;
        }


        private void RemoveDestButton()
        {
            System.Windows.Controls.Button ee = DestinationstView.SelectedItem as System.Windows.Controls.Button;
            if (ee == null)
            {
                SetupOn = false;
                MoveCopyCheckBox.IsChecked = false;
                return;
            }
            Seldir = ee.Content as string;

            DestinationstView.Items.Remove(ee);

            var qq = from MovePath cc in Context.MovePaths
                     where cc.Path == Seldir
                     select cc;

            if (qq.Any())
            {
                Context.MovePaths.Remove(qq.First());
                Context.SaveChanges();
            }
            InvalidateArrange();
            InvalidateMeasure();
            InvalidateVisual();
            SetupOn = false;
            MoveCopyCheckBox.IsChecked = false;
        }


        private void RemoveDestButton_Click(object sender, RoutedEventArgs e)
        {
            MoveCopyCheckBox.IsChecked = null;
            SetupOn = true;

        }


        public void LoadDestinationPaths()
        {
            var qq = from MovePath cc in Context.MovePaths
                     select cc;

            if (qq.Any() == false)
            {
                return;
            }

            DestinationstView.Items.Clear();

            foreach (MovePath ff in qq)
            {
                if (Directory.Exists(ff.Path) == false)
                {
                    continue;
                }

                string bname = "ZA" + rand.Next().ToString();

                System.Windows.Controls.Button isdir = new System.Windows.Controls.Button
                {
                    Width = 300,
                    Height = 27,
                    Name = bname,
                };
                isdir.Content = ff.Path;
                isdir.Click += MoveSelectedFileClick;
                DestinationstView.Items.Add(isdir);
            }
            InvalidateArrange();
            InvalidateMeasure();
            InvalidateVisual();
        }

        private void DestinationstView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (DestinationstView.SelectedItem != null)
            {
                SelectedCopyDestination = (DestinationstView.SelectedItem as System.Windows.Controls.Button).Content as string;
            }

            if (e.AddedItems.Count != 0)
            {
                System.Windows.Controls.Button aa = e.AddedItems[0] as System.Windows.Controls.Button;
                //   aa.Background = new SolidColorBrush(Color.FromArgb(255, 40,40, 127));

                aa.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 40, 40, 177));

                aa.BorderThickness = new Thickness(4, 4, 4, 4);
                aa.InvalidateVisual();

            }

            if (e.RemovedItems.Count != 0)
            {
                System.Windows.Controls.Button aa = e.RemovedItems[0] as System.Windows.Controls.Button;
                //    aa.Background = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40));
                aa.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40));

                aa.BorderThickness = new Thickness(1, 1, 1, 1);
                aa.InvalidateVisual();
            }
        }
        #endregion

        #region nexttagged

        /// <summary>
        ///  finds next in view with a tag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void FindNextTaggedButton_Click(object sender, RoutedEventArgs e)
        //{

        //    int fnd;
        //    int start = 0;
        //    int cnt = FilesListView.Items.Count;
        //    if (cnt == 0)
        //    {
        //        return;
        //    }
        //    if (FilesListView.SelectedItem == null)
        //    {
        //        start = 0;
        //    }
        //    else
        //    {
        //        start = FilesListView.Items.IndexOf(FilesListView.SelectedItem);
        //    }

        //    for (int ii = 0; ii < cnt; ii++)
        //    {
        //        int kk = (ii + 1 + start) % cnt;
        //        TagFileInfo tt = FilesListView.Items[kk] as TagFileInfo;

        //        if (tt != null)
        //        {

        //            var uu = from ww in Context.TagFileInfos
        //                     where ww.FilePath == tt.FilePath
        //                     select ww;

        //            if (uu.Any())
        //            {

        //                TagFileInfo ss = uu.First();

        //                if (ss.TagInfos.Count > 0)
        //                {
        //                    //   tt = ss;
        //                    fnd = FilesListView.Items.IndexOf(tt);
        //                    FilesListView.SelectedIndex = fnd;
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //}



        private void ShowTaggedButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilesListView.Items.Count == 0)
            {
                return;
            }

            int fnd;

            foreach (var vv in FilesListView.Items)
            {
                TagFileInfo tt = vv as TagFileInfo;
                var uu = from ww in Context.TagFileInfos
                         where ww.FilePath == tt.FilePath
                         select ww;

                if (uu.Any())
                {

                    TagFileInfo ss = uu.First();

                    if (ss.TagInfos.Count > 0)
                    {
                        fnd = FilesListView.Items.IndexOf(ss);
                        FilesListView.SelectedIndex = fnd;

                        break;
                    }
                }

            }
        }

        #endregion



        private void DuplicateCheckSums_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CopyTaggedFiles_Click(object sender, RoutedEventArgs e)
        {
            CopyFilesWithSelectedTags();
        }

        private void MoveTaggedFiles_Click(object sender, RoutedEventArgs e)
        {
            MoveFilesWithSelectedTags();
        }

        private void DeleteTheSelected_Click(object sender, RoutedEventArgs e)
        {
            DeleteFile(SelectedFileInfo);
        }

        private void ReadTagsTextFile_Click(object sender, RoutedEventArgs e)
        {
            char[] charsToTrim = { '*', ' ', '\'', ',', '\\', ',', '.', };

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = $"Taglist"; // Default file name
            dlg.DefaultExt = ".txt"; // Default file extension
            dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                Stream st = null;
                try
                {
                    st = dlg.OpenFile();

                    using (StreamReader sr = new StreamReader(st))
                    {
                        string ff = string.Empty;
                        while ((ff = sr.ReadLine()) != null)
                        {
                            ff = ff.Trim(charsToTrim);
                            ff = ff.Trim();
                            //                         ff.Trim(',');
                            var cc = from nn in Context.TagInfos
                                     where nn.Tag == ff
                                     select nn;

                            if (cc.Any())
                            {
                                continue;
                            }
                            else
                            {
                                TagInfo tt = new TagInfo()
                                {
                                    Tag = ff,
                                };

                                Context.TagInfos.Add(tt);
                                Context.SaveChanges();
                                TagsListView.Items.Add(tt);
                            }
                        }
                    }
                }
                finally
                {
                    if (st != null)
                        st.Dispose();
                }
            }
        }

        private void WriteTagsTextFile_Click(object sender, RoutedEventArgs e)
        {

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = $"Taglist"; // Default file name
            dlg.DefaultExt = ".txt"; // Default file extension
            dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension
            dlg.CheckFileExists = false;

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                string fileName = dlg.FileName;

                try
                {

                    using (StreamWriter outputFile = new StreamWriter(fileName, false))
                    {
                        var cc = from nn in Context.TagInfos
                                 select nn;
                        if (cc.Any())
                        {
                            foreach (TagInfo ti in cc)
                            {
                                outputFile.WriteLine(ti.Tag);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message, " write tags", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
        }


        private void AddChosenFile(TagFileInfo chosen)
        {
            var ff = from su in Context.TagFileInfos
                     where su.FilePath == chosen.FilePath
                     select su;

            // add crc to new and old 
            if (ff.Any())
            {
                chosen = ff.First();
                chosen.Crc32 = CalculateCRC(chosen);
                Context.TagFileInfos.Update(chosen);
                Context.SaveChanges();
                return;
            }

            FileInfo fi1 = new(chosen.FilePath);

            chosen.FileSize = fi1.Length;
            chosen.FileExtension = fi1.Extension;
            chosen.Crc32 = CalculateCRC(fi1);


            Context.TagFileInfos.Add(chosen);
            Context.SaveChanges();
        }


        TagInfo FullTagInfo;
        private void AddTagFromFileName(object sender, RoutedEventArgs e)
        {
            var ff = from TagInfo su in TagsListView.SelectedItems
                     select su;
            if (ff.Any() == false)
            {
                return;
            }
            if (ff.Count() > 1)
            {
                return;
            }

            FullTagInfo = ff.First();

            FullTag = FullTagInfo.Tag;

            TagSearchPopup.IsOpen = true;

            //string firstword = tt.Tag.Split(' ').First();
            //if (string.IsNullOrEmpty(firstword) == true)
            //{
            //    firstword = tt.Tag;
            //}

            //foreach (TagFileInfo a in FilesListView.Items)
            //{
            //    if (a.FileName.Contains(firstword, StringComparison.CurrentCultureIgnoreCase))
            //    {
            //        AddChosenFile(a);

            //        // reload it
            //        var gg = from su in Context.TagFileInfos
            //                 where su.FilePath == a.FilePath
            //                 select su;

            //        TagFileInfo work;
            //        if (gg.Any())
            //        {
            //            work = gg.First();

            //            var qq = from tftf in Context.TagFileInfoTagInfos
            //                     where tftf.TagInfoId == tt.Id &&
            //                     tftf.TagFileInfoId == work.Id
            //                     select tftf;

            //            // if not add it
            //            if (qq.Any() == false)
            //            {
            //                var rr = new TagFileInfoTagInfo
            //                {
            //                    TagFileInfoId = work.Id,
            //                    TagInfoId = tt.Id,
            //                };

            //                Context.TagFileInfoTagInfos.Add(rr);
            //                Context.SaveChanges();
            //            }
            //        }
            //    }

            //}
        }

        void WhenFileWithSameNameandCRCExists(object sender, RoutedEventArgs e)
        {
            if (FilesListView == null)
            {
                return;
            }
            foreach (var wa in FilesListView.Items)
            {
                if (wa == null)
                {
                    continue;
                }
                TagFileInfo aa = wa as TagFileInfo;
                var dbf = from ss in Context.TagFileInfos
                          where ss.FileName == aa.FileName && ss.FilePath != aa.FilePath
                          select ss;

                if (dbf.Any())
                {
                    TagFileInfo samename = dbf.First();

                    AddChosenFile(aa);


                    // reload aa as gg
                    var gg = from su in Context.TagFileInfos
                             where su.FilePath == aa.FilePath
                             select su;

                    if (gg.Any())
                    {
                        TagFileInfo listfile = gg.First();


                        if (samename.Crc32 == listfile.Crc32 && samename.FileSize == listfile.FileSize)
                        {

                            var qq = from atftf in Context.TagFileInfoTagInfos
                                     where atftf.TagFileInfoId == samename.Id
                                     select atftf;

                            if (qq.Any())
                            {
                                foreach (TagFileInfoTagInfo otfitf in qq)
                                {
                                    var chk = from rr in Context.TagFileInfoTagInfos
                                              where rr.TagFileInfoId == listfile.Id && rr.TagInfoId == otfitf.TagInfo.Id
                                              select rr;

                                    if (chk.Any() == false)
                                    {

                                        var rr = new TagFileInfoTagInfo
                                        {
                                            TagFileInfoId = listfile.Id,
                                            TagInfoId = otfitf.TagInfo.Id,
                                        };

                                        Context.TagFileInfoTagInfos.Add(rr);
                                        Context.SaveChanges();
                                    }
                                }
                            }

                        }

                    }

                }

            }

        }

        public async void FillMediaInfo(string path)
        {

            mediaInfo = new Dictionary<string, string>();
            var media = new LibVLCSharp.Shared.Media(LibVLC, path);
            await media.Parse(MediaParseOptions.FetchLocal);

            string? inf = null;

            inf = media.Meta(MetadataType.Title);
            if (inf != null)
            {
                mediaInfo["Title"] = inf;
            }

            inf = media.Meta(MetadataType.Artist);
            if (inf != null)
            {
                mediaInfo["Artist"] = inf;
            }
            inf = media.Meta(MetadataType.Genre);
            if (inf != null)
            {
                mediaInfo["Genre"] = inf;
            }
            inf = media.Meta(MetadataType.Copyright);
            if (inf != null)
            {
                mediaInfo["Copyright"] = inf;

            }
            inf = media.Meta(MetadataType.Album);
            if (inf != null)
            {
                mediaInfo["Album"] = inf;

            }
            inf = media.Meta(MetadataType.TrackNumber);
            if (inf != null)
            {
                mediaInfo["TrackNumber"] = inf;

            }
            inf = media.Meta(MetadataType.Description);
            if (inf != null)
            {
                mediaInfo["Description"] = inf;

            }
            inf = media.Meta(MetadataType.Rating);
            if (inf != null)
            {
                mediaInfo["Rating"] = inf;

            }
            inf = media.Meta(MetadataType.Date);
            if (inf != null)
            {
                mediaInfo["Date"] = inf;

            }
            inf = media.Meta(MetadataType.Setting);
            if (inf != null)
            {
                mediaInfo["Setting"] = inf;

            }
            inf = media.Meta(MetadataType.URL);
            if (inf != null)
            {
                mediaInfo["URL"] = inf;

            }
            inf = media.Meta(MetadataType.Language);
            if (inf != null)
            {
                mediaInfo["Language"] = inf;

            }
            inf = media.Meta(MetadataType.NowPlaying);
            if (inf != null)
            {
                mediaInfo["NowPlaying"] = inf;

            }
            inf = media.Meta(MetadataType.Publisher);
            if (inf != null)
            {
                mediaInfo["Publisher"] = inf;

            }
            inf = media.Meta(MetadataType.EncodedBy);
            if (inf != null)
            {
                mediaInfo["EncodedBy"] = inf;

            }
            inf = media.Meta(MetadataType.ArtworkURL);
            if (inf != null)
            {
                mediaInfo["ArtworkURL"] = inf;

            }
            inf = media.Meta(MetadataType.TrackID);
            if (inf != null)
            {
                mediaInfo["TrackID"] = inf;

            }
            inf = media.Meta(MetadataType.TrackTotal);
            if (inf != null)
            {
                mediaInfo["TrackTotal"] = inf;

            }
            inf = media.Meta(MetadataType.Director);
            if (inf != null)
            {
                mediaInfo["Director"] = inf;

            }
            inf = media.Meta(MetadataType.Season);
            if (inf != null)
            {
                mediaInfo["Season"] = inf;

            }
            inf = media.Meta(MetadataType.Episode);
            if (inf != null)
            {
                mediaInfo["Episode"] = inf;

            }
            inf = media.Meta(MetadataType.ShowName);
            if (inf != null)
            {
                mediaInfo["ShowName"] = inf;

            }
            inf = media.Meta(MetadataType.Actors);
            if (inf != null)
            {
                mediaInfo["Actors"] = inf;

            }
            inf = media.Meta(MetadataType.AlbumArtist);
            if (inf != null)
            {
                mediaInfo["AlbumArtist"] = inf;

            }
            inf = media.Meta(MetadataType.DiscNumber);
            if (inf != null)
            {
                mediaInfo["DiscNumber"] = inf;

            }
            inf = media.Meta(MetadataType.DiscTotal);
            if (inf != null)
            {
                mediaInfo["DiscTotal"] = inf;

            }

            foreach (var track in media.Tracks)
            {

                switch (track.TrackType)
                {
                    case TrackType.Audio:

                        mediaInfo[$"{nameof(track.Data.Audio.Channels)}"] = $"{track.Data.Audio.Channels}";
                        mediaInfo[$"{nameof(track.Data.Audio.Rate)}"] =  $"{track.Data.Audio.Rate}";
                        break;
                    case TrackType.Video:

                        mediaInfo[$"{nameof(track.Data.Video.FrameRateNum)}"] =  $"{track.Data.Video.FrameRateNum}";
                        mediaInfo[$"{nameof(track.Data.Video.FrameRateDen)}"] =  $"{track.Data.Video.FrameRateDen}";
                        mediaInfo[$"{nameof(track.Data.Video.Height)}"] =  $"{track.Data.Video.Height}";
                        mediaInfo[$"{nameof(track.Data.Video.Width)}"] =  $"{track.Data.Video.Width}";
                        break;
                }
            }

            FileInfoList.Items.Clear();
            foreach (var kk in mediaInfo.Keys)
            {
                string aa = kk + " is " + mediaInfo[kk];

                FileInfoList.Items.Add(aa);
            }
            FileInfoList.InvalidateVisual();
        }

        private void MoveCopyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SetupOn = false;
            MoveCopyCheckBox.Content = "Move";
        }

        private void MoveCopyCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetupOn = false;
            MoveCopyCheckBox.Content = "Copy";
        }

        private void MoveCopyCheckBox_Indeterminate(object sender, RoutedEventArgs e)
        {
            SetupOn = true;
            MoveCopyCheckBox.Content = "Remove Destination";
        }


        #region SearcgFileForTag
        bool _firstOne = false;

        bool _lastOne = false;

        bool _phrase = false;

        bool _custom = false;

        string? _fullTag = null;
        public string? FullTag
        {
            get { return _fullTag; }
            set
            {
                _fullTag = value;
                TagString.Text = value;
                FirstOne = true;
            }

        }

        public bool FirstOne
        {
            get
            {
                return _firstOne;
            }
            set
            {
                _firstOne = value;

                if (value)
                {
                    string[] partsoftag;
                    if (_fullTag != null)
                    {
                        partsoftag = _fullTag.Split(' ', 5, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        FirstOneCheck.IsChecked = true;
                        LastOneCheck.IsChecked = false;
                        PhraseCheck.IsChecked = false;
                        CustomCheck.IsChecked = false;
                        SearchText.Text = partsoftag[0];
                    }
                }
            }
        }

        public bool LastOne
        {
            get
            {
                return _lastOne;
            }
            set
            {
                _lastOne = value;

                if (value)
                {
                    string[] partsoftag;
                    if (_fullTag != null)
                    {
                        partsoftag = _fullTag.Split(' ', 5, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        LastOneCheck.IsChecked = true;
                        FirstOneCheck.IsChecked = false;
                        PhraseCheck.IsChecked = false;
                        CustomCheck.IsChecked = false;
                        SearchText.Text =  partsoftag.Last();
                    }
                }
            }
        }

        public bool Phrase
        {
            get
            {
                return _phrase;
            }
            set
            {
                _phrase = value;
                if (value)
                {

                    if (_fullTag != null)
                    {
                        LastOneCheck.IsChecked = false;
                        FirstOneCheck.IsChecked = false;
                        PhraseCheck.IsChecked = true;
                        CustomCheck.IsChecked = false;
                        SearchText.Text =  _fullTag;
                    }
                }
            }
        }

        public bool Custom
        {

            get
            {
                return _custom;
            }
            set
            {
                _custom = value;
                if (value)
                {
                    if (_fullTag != null)
                    {
                        LastOneCheck.IsChecked = false;
                        FirstOneCheck.IsChecked = false;
                        PhraseCheck.IsChecked = false;
                        CustomCheck.IsChecked = true;
                        //  SearchText.Text =  ;
                    }
                }
            }
        }

        private void FirstOneCheck_Checked(object sender, RoutedEventArgs e)
        {
            FirstOne = true;
        }

        private void LastOneCheck_Checked(object sender, RoutedEventArgs e)
        {
            LastOne = true;
        }

        private void PhraseCheck_Checked(object sender, RoutedEventArgs e)
        {
            Phrase = true;
        }

        private void CustomCheck_Checked(object sender, RoutedEventArgs e)
        {
            Custom = true;
        }

        public void DoSearch_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(SearchText.Text) == true)
            {
                return;
            }

            foreach (TagFileInfo a in FilesListView.Items)
            {
                if (a.FileName.Contains(SearchText.Text, StringComparison.CurrentCultureIgnoreCase))
                {
                    AddChosenFile(a);

                    // reload it
                    var gg = from su in Context.TagFileInfos
                             where su.FilePath == a.FilePath
                             select su;

                    TagFileInfo work;
                    if (gg.Any())
                    {
                        work = gg.First();

                        var qq = from tftf in Context.TagFileInfoTagInfos
                                 where tftf.TagInfoId == FullTagInfo.Id &&
                                 tftf.TagFileInfoId == work.Id
                                 select tftf;

                        // if not add it
                        if (qq.Any() == false)
                        {
                            var rr = new TagFileInfoTagInfo
                            {
                                TagFileInfoId = work.Id,
                                TagInfoId = FullTagInfo.Id,
                            };

                            Context.TagFileInfoTagInfos.Add(rr);
                            Context.SaveChanges();
                        }
                    }
                }

            }
            TagSearchPopup.IsOpen = false;
        }

        private void NoSearch_Click(object sender, RoutedEventArgs e)
        {
            TagSearchPopup.IsOpen = false;
        }
        #endregion
    }
}
