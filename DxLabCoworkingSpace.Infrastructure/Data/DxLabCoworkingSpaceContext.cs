using System;
using System.Collections.Generic;
using DxLabCoworkingSpace;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;

namespace DXLAB_Coworking_Space_Booking_System
{
    public partial class DxLabCoworkingSpaceContext : DbContext
    {
        public DxLabCoworkingSpaceContext()
        {
        }

        public DxLabCoworkingSpaceContext(DbContextOptions<DxLabCoworkingSpaceContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Area> Areas { get; set; } = null!;
        public virtual DbSet<Blog> Blogs { get; set; } = null!;
        public virtual DbSet<Booking> Bookings { get; set; } = null!;
        public virtual DbSet<BookingDetail> BookingDetails { get; set; } = null!;
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
        public virtual DbSet<AreaType> AreaTypes { get; set; } = null!;
        public virtual DbSet<UsingFacility> UsingFacilities { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var builder = new ConfigurationBuilder()
                              .SetBasePath(Directory.GetCurrentDirectory())
                              .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfigurationRoot configuration = builder.Build();
                optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Area>(entity =>
            {
                entity.HasOne(d => d.Room)
                    .WithMany(p => p.Areas)
                    .HasForeignKey(d => d.RoomId)
                    .HasConstraintName("FK_Areas_Rooms");
            });

            modelBuilder.Entity<Blog>(entity =>
            {
                entity.Property(e => e.BlogCreatedDate).HasColumnType("date");

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
                entity.HasIndex(e => e.SlotId, "UQ_BookingDetails_SlotId")
                    .IsUnique();

                entity.Property(e => e.CheckinTime).HasColumnType("datetime");

                entity.Property(e => e.CheckoutTime).HasColumnType("datetime");

                entity.HasOne(d => d.Booking)
                    .WithMany(p => p.BookingDetails)
                    .HasForeignKey(d => d.BookingId)
                    .HasConstraintName("FK_BookingDetails_Bookings");

                entity.HasOne(d => d.Slot)
                    .WithOne(p => p.BookingDetail)
                    .HasForeignKey<BookingDetail>(d => d.SlotId)
                    .HasConstraintName("FK_BookingDetails_Slots");
            });

            modelBuilder.Entity<FacilitiesStatus>(entity =>
            {
                entity.HasKey(e => e.FacilityStatusId);

                entity.ToTable("FacilitiesStatus");

                entity.Property(e => e.FacilityStatusId).HasMaxLength(50);

                entity.Property(e => e.BatchNumber).HasMaxLength(50);

                entity.HasOne(d => d.Facility)
                    .WithMany(p => p.FacilitiesStatuses)
                    .HasForeignKey(d => new { d.FailityId, d.BatchNumber })
                    .HasConstraintName("FK_FacilitiesStatus_Facilities");
            });

            modelBuilder.Entity<Facility>(entity =>
            {
                entity.HasKey(e => new { e.FacilityId, e.BatchNumber })
                    .HasName("PK_Facilities_1");

                entity.Property(e => e.FacilityId).ValueGeneratedOnAdd();

                entity.Property(e => e.BatchNumber).HasMaxLength(50);

                entity.Property(e => e.Cost).HasColumnType("decimal(10, 2)");

                entity.Property(e => e.ExpiredTime).HasColumnType("datetime");

                entity.Property(e => e.ImportDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<Image>(entity =>
            {
                entity.Property(e => e.ImageUrl)
                    .HasMaxLength(255)
                    .HasColumnName("ImageURL");

                entity.HasOne(d => d.AreaType)
                    .WithMany(p => p.Images)
                    .HasForeignKey(d => d.AreaTypeId)
                    .HasConstraintName("FK_Images_AreaTypes");

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

                entity.HasOne(d => d.Booking)
                    .WithMany(p => p.Notifications)
                    .HasForeignKey(d => d.BookingId)
                    .HasConstraintName("FK_Notifications_Bookings");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Notifications)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_Notifications_Users");
            });

            modelBuilder.Entity<Position>(entity =>
            {
                entity.HasIndex(e => e.BookingDetailId, "UQ_Positions_BookingDetailId")
                    .IsUnique();

                entity.Property(e => e.PositionName).HasMaxLength(50);

                entity.Property(e => e.UsingFacilityId).HasMaxLength(50);

                entity.HasOne(d => d.Area)
                    .WithMany(p => p.Positions)
                    .HasForeignKey(d => d.AreaId)
                    .HasConstraintName("FK_Positions_Areas");

                entity.HasOne(d => d.BookingDetail)
                    .WithOne(p => p.Position)
                    .HasForeignKey<Position>(d => d.BookingDetailId)
                    .HasConstraintName("FK_Positions_BookingDetails");
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
                entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");

                entity.Property(e => e.RoomName).HasMaxLength(50);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Avatar).HasMaxLength(255);

                entity.Property(e => e.Email).HasMaxLength(255);

                entity.Property(e => e.FullName).HasMaxLength(100);

                entity.Property(e => e.WalletAddress).HasMaxLength(255);

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("FK_Users_Roles");
            });

            modelBuilder.Entity<UsingFacility>(entity =>
            {
                entity.Property(e => e.UsingFacilityId).HasMaxLength(50);

                entity.Property(e => e.BatchNumber).HasMaxLength(50);

                entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");

                entity.HasOne(d => d.Area)
                    .WithMany(p => p.UsingFacilities)
                    .HasForeignKey(d => d.AreaId)
                    .HasConstraintName("FK_UsingFacilities_Areas");

                entity.HasOne(d => d.Facility)
                    .WithMany(p => p.UsingFacilities)
                    .HasForeignKey(d => new { d.FacilityId, d.BatchNumber })
                    .HasConstraintName("FK_UsingFacilities_Facilities");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
