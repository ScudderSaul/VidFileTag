using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
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

        }

        public virtual DbSet<MiscInfo> MiscInfos { get; set; }

        public virtual DbSet<MovePath> MovePaths { get; set; }

        public virtual DbSet<TagInfo> TagInfos { get; set; }
        public virtual DbSet<TagFileInfo> TagFileInfos { get; set; }

        public virtual DbSet<TagFileInfoTagInfo> TagFileInfoTagInfos { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=dbFileTag.db");



        }



    }

}
