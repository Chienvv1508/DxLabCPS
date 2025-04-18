using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DxLabCoworkingSpace
{
    public partial class DxLabSystemContext : DbContext
    {
        public DxLabSystemContext()
        {
        }

        public DxLabSystemContext(DbContextOptions<DxLabSystemContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Area> Areas { get; set; } = null!;
        public virtual DbSet<AreaType> AreaTypes { get; set; } = null!;
        public virtual DbSet<Blog> Blogs { get; set; } = null!;
        public virtual DbSet<Booking> Bookings { get; set; } = null!;
        public virtual DbSet<BookingDetail> BookingDetails { get; set; } = null!;
        public virtual DbSet<ContractCrawl> ContractCrawls { get; set; } = null!;
        public virtual DbSet<FacilitiesStatus> FacilitiesStatuses { get; set; } = null!;
        public virtual DbSet<Facility> Facilities { get; set; } = null!;
        public virtual DbSet<Image> Images { get; set; } = null!;
        public virtual DbSet<Notification> Notifications { get; set; } = null!;
        public virtual DbSet<Position> Positions { get; set; } = null!;
        public virtual DbSet<Report> Reports { get; set; } = null!;
        public virtual DbSet<Role> Roles { get; set; } = null!;
        public virtual DbSet<Room> Rooms { get; set; } = null!;
        public virtual DbSet<Slot> Slots { get; set; } = null!;
        public virtual DbSet<User> Users { get; set; } = null!;
        public virtual DbSet<UsingFacility> UsingFacilities { get; set; } = null!;
        public virtual DbSet<SumaryExpense> SumaryExpenses { get; set; } = null!;
        public virtual DbSet<AreaTypeCategory> AreaTypeCategory { get; set; } = null!;
        public virtual DbSet<UltilizationRate> UltilizationRate { get; set; } = null!;
        public virtual DbSet<DepreciationSum> DepreciationSums { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Server=DESKTOP-9KB6FKU\\SQLEXPRESS;Database=DxLabSystem; uid = sa; pwd = 123");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Area>(entity =>
            {
                entity.Property(e => e.AreaDescription).HasMaxLength(255);
                entity.Property(e => e.AreaName).HasMaxLength(250);

                entity.HasOne(d => d.AreaType)
                    .WithMany(p => p.Areas)
                    .HasForeignKey(d => d.AreaTypeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Areas_AreaTypes");

                entity.HasOne(d => d.Room)
                    .WithMany(p => p.Areas)
                    .HasForeignKey(d => d.RoomId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Areas_Rooms");
            });

            modelBuilder.Entity<AreaType>(entity =>
            {
                entity.Property(e => e.AreaDescription).HasMaxLength(250);

                entity.Property(e => e.AreaTypeName).HasMaxLength(250);

                entity.Property(e => e.Status).HasColumnName("Status");

                entity.Property(e => e.Price).HasColumnType("decimal(10, 0)");

                entity.HasOne(d => d.AreaTypeCategory)
                   .WithMany(p => p.AreaTypes)
                   .HasForeignKey(d => d.AreaCategory)
                   .OnDelete(DeleteBehavior.ClientSetNull)
                   .HasConstraintName("FK_AreaTypes_AreaTypeCategory");
            });

            modelBuilder.Entity<Blog>(entity =>
            {
                entity.Property(e => e.BlogCreatedDate).HasPrecision(0);

                entity.Property(e => e.BlogTitle).HasMaxLength(50);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Blogs)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_Blogs_Users");
            });

            modelBuilder.Entity<Booking>(entity =>
            {
                entity.Property(e => e.BookingCreatedDate).HasColumnType("datetime");

                entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Bookings)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_Bookings_Users");
            });

            modelBuilder.Entity<BookingDetail>(entity =>
            {


                entity.Property(e => e.CheckinTime).HasColumnType("datetime");

                entity.Property(e => e.CheckoutTime).HasColumnType("datetime");

                entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");

                entity.HasOne(d => d.Area)
                    .WithMany(p => p.BookingDetails)
                    .HasForeignKey(d => d.AreaId)
                    .HasConstraintName("FK_BookingDetails_Areas");

                entity.HasOne(d => d.Booking)
                    .WithMany(p => p.BookingDetails)
                    .HasForeignKey(d => d.BookingId)
                    .HasConstraintName("FK_BookingDetails_Bookings");

                entity.HasOne(d => d.Position)
                    .WithMany(p => p.BookingDetails)
                    .HasForeignKey(d => d.PositionId)
                    .HasConstraintName("FK_BookingDetails_Positions");

                entity.HasOne(d => d.Slot)
                    .WithMany(p => p.BookingDetails)
                    .HasForeignKey(d => d.SlotId)
                    .HasConstraintName("FK_BookingDetails_Slots")
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ContractCrawl>(entity =>
            {
                entity.HasKey(e => e.ContractCrawlId);

                entity.ToTable("ContractCrawl");

                entity.Property(e => e.ContractAddress).HasMaxLength(50);

                entity.Property(e => e.ContractName).HasMaxLength(50);

                entity.Property(e => e.LastBlock).HasMaxLength(50);
            });

            modelBuilder.Entity<FacilitiesStatus>(entity =>
            {
                entity.HasKey(e => e.FacilityStatusId);

                entity.ToTable("FacilitiesStatus");

                entity.Property(f => f.FacilityStatusId)
                 .ValueGeneratedOnAdd();

                entity.Property(e => e.BatchNumber).HasMaxLength(50);

                entity.HasOne(d => d.Facility)
                    .WithMany(p => p.FacilitiesStatuses)
                    .HasForeignKey(d => new { d.FacilityId, d.BatchNumber, d.ImportDate })
                    .HasConstraintName("FK_FacilitiesStatus_Facilities");
            });
            modelBuilder.Entity<DepreciationSum>(entity =>
            {
                entity.ToTable("DepreciationSum");

                entity.Property(e => e.BatchNumber).HasMaxLength(50);

                entity.Property(e => e.DepreciationAmount).HasColumnType("money");

                entity.Property(e => e.ImportDate).HasColumnType("datetime");

                entity.Property(e => e.SumDate).HasColumnType("datetime");

                entity.HasOne(d => d.Facility)
                    .WithMany(p => p.DepreciationSums)
                    .HasForeignKey(d => new { d.FacilityId, d.BatchNumber, d.ImportDate })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_DepreciationSum_Facilities");
            });

            modelBuilder.Entity<Facility>(entity =>
            {
                entity.HasKey(e => new { e.FacilityId, e.BatchNumber, e.ImportDate });

                entity.Property(e => e.FacilityId).ValueGeneratedOnAdd();

                entity.Property(e => e.BatchNumber).HasMaxLength(50);

                entity.Property(e => e.ImportDate).HasColumnType("datetime");

                entity.Property(e => e.Cost).HasColumnType("decimal(10, 2)");

                entity.Property(e => e.ExpiredTime).HasColumnType("datetime");

                entity.Property(e => e.RemainingValue).HasColumnType("decimal(10, 2)");
            });

            modelBuilder.Entity<Image>(entity =>
            {
                entity.Property(e => e.ImageUrl)
                    .HasMaxLength(255)
                    .HasColumnName("ImageURL");

                entity.HasOne(d => d.Area)
                    .WithMany(p => p.Images)
                    .HasForeignKey(d => d.AreaId)
                    .HasConstraintName("FK_Images_Areas");
                entity.HasOne(d => d.AreaTypeCategory)
                   .WithMany(p => p.Images)
                   .HasForeignKey(d => d.AreaTypeCategoryId)
                   .HasConstraintName("FK_Images_AreaTypeCategory");

                entity.HasOne(d => d.AreaType)
                    .WithMany(p => p.Images)
                    .HasForeignKey(d => d.AreaTypeId)
                    .HasConstraintName("FK_Images_AreaType");

                entity.HasOne(d => d.Blog)
                    .WithMany(p => p.Images)
                    .HasForeignKey(d => d.BlogId)
                    .HasConstraintName("FK_Images_Blogs");

                entity.HasOne(d => d.Room)
                    .WithMany(p => p.Images)
                    .HasForeignKey(d => d.RoomId)
                    .HasConstraintName("FK_Images_Rooms");
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.Message).HasMaxLength(255);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Notifications)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_Notifications_Users");
            });

            modelBuilder.Entity<Position>(entity =>
            {
                entity.HasOne(d => d.Area)
                    .WithMany(p => p.Positions)
                    .HasForeignKey(d => d.AreaId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Positions_Areas");
            });

            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasIndex(e => e.BookingDetailId, "UQ_Reports_BookingDetailId")
                    .IsUnique();

                entity.Property(e => e.CreatedDate).HasColumnType("datetime");

                entity.HasOne(d => d.BookingDetail)
                    .WithOne(p => p.Report)
                    .HasForeignKey<Report>(d => d.BookingDetailId)
                    .HasConstraintName("FK_Reports_BookingDetails");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Reports)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_Reports_Users");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.Property(e => e.RoleName).HasMaxLength(55);
            });

            modelBuilder.Entity<Room>(entity =>
            {
                entity.Property(e => e.Status).HasColumnName("Status");

                entity.Property(e => e.RoomName).HasMaxLength(50);
            });

            modelBuilder.Entity<User>(entity =>
            {

                entity.Property(e => e.Email).HasMaxLength(255);

                entity.Property(e => e.FullName).HasMaxLength(100);

                entity.Property(e => e.WalletAddress).HasMaxLength(255);

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("FK_Users_Roles");
            });

            modelBuilder.Entity<UltilizationRate>(entity =>
            {
                entity.ToTable("UltilizationRate");

                entity.Property(e => e.AreaName).HasMaxLength(250);

                entity.Property(e => e.AreaTypeCategoryTitle).HasMaxLength(250);

                entity.Property(e => e.AreaTypeName).HasMaxLength(250);

                entity.Property(e => e.Rate).HasColumnType("decimal(3, 2)");

                entity.Property(e => e.RoomName).HasMaxLength(50);

                entity.Property(e => e.THDate)
                    .HasColumnType("datetime")
                    .HasColumnName("THDate");
            });

            modelBuilder.Entity<SumaryExpense>(entity =>
            {
                entity.Property(e => e.Amout).HasColumnType("money");

                entity.Property(e => e.SumaryDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<UsingFacility>(entity =>
            {
                entity.Property(e => e.UsingFacilityId).HasMaxLength(50);

                entity.Property(e => e.BatchNumber).HasMaxLength(50);



                entity.HasOne(d => d.Area)
                    .WithMany(p => p.UsingFacilities)
                    .HasForeignKey(d => d.AreaId)
                    .HasConstraintName("FK_UsingFacilities_Areas");

                entity.HasOne(d => d.Facility)
                    .WithMany(p => p.UsingFacilities)
                    .HasForeignKey(d => new { d.FacilityId, d.BatchNumber, d.ImportDate })
                    .HasConstraintName("FK_UsingFacilities_Facilities");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
