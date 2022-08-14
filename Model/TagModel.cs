using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace VidFileTag.Model
{

    public partial class TagModel : DbContext
    {
        public TagModel() : base()
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TagFileInfoTagInfo>().HasKey(i => new { i.TagFileInfoId, i.TagInfoId });
            modelBuilder.Entity<TagInfoTagSetInfo>().HasKey(i => new { i.TagInfoId, i.TagSetInfoId });
        }

        public virtual DbSet<MiscInfo> MiscInfos { get; set; }

        public virtual DbSet<MovePath> MovePaths { get; set; }

        public virtual DbSet<TagInfo> TagInfos { get; set; }
        public virtual DbSet<TagSetInfo> TagSetInfos { get; set; }
        public virtual DbSet<TagFileInfo> TagFileInfos { get; set; }

        public virtual DbSet<TagFileInfoTagInfo> TagFileInfoTagInfos { get; set; }

        public virtual DbSet<TagInfoTagSetInfo> TagInfoTagSetInfos { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            // would need admin setup
            //if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + $"\\VidFileTag") == false)
            //{
            //    Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + $"\\VidFileTag");
            //}

            //string apath = "Filename=" + Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + $"\\VidFileTag\\dbVidFileTag.db";


            if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + $"\\VidFileTag") == false)
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + $"\\VidFileTag");
            }
            string apath = "Filename=" + Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + $"\\VidFileTag\\dbVidFileTag.db";
            optionsBuilder.UseSqlite(apath);
        }
    }

}
