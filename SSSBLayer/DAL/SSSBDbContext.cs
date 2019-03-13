using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace TaskBroker.SSSB.EF
{
    public partial class SSSBDbContext : DbContext
    {
        public SSSBDbContext()
        {
        }

        public SSSBDbContext(DbContextOptions<SSSBDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Executor> Executor { get; set; }
        public virtual DbSet<MetaData> MetaData { get; set; }
        public virtual DbSet<OnDemandTask> OnDemandTask { get; set; }
        public virtual DbSet<Setting> Setting { get; set; }
        public virtual DbSet<Shedule> Shedule { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=.;Database=temp;Integrated Security=False;User ID=sa;Password=***");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.0-rtm-35687");

            modelBuilder.Entity<Executor>(entity =>
            {
                entity.ToTable("Executor", "PPS");

                entity.HasIndex(e => e.FullTypeName)
                    .HasName("UK_Executor")
                    .IsUnique();

                entity.Property(e => e.ExecutorId)
                    .HasColumnName("ExecutorID")
                    .ValueGeneratedNever();

                entity.Property(e => e.Active)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.Description)
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.ExecutorSettingsSchema)
                    .IsRequired()
                    .HasColumnType("xml");

                entity.Property(e => e.FullTypeName)
                    .IsRequired()
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.RowTimeStamp)
                    .IsRequired()
                    .IsRowVersion();
            });

            modelBuilder.Entity<MetaData>(entity =>
            {
                entity.ToTable("MetaData", "PPS");

                entity.HasIndex(e => e.Context)
                    .HasName("IX_Meta_Context")
                    .IsUnique();

                entity.HasIndex(e => e.CreateDate)
                    .HasName("IX_Meta_CreateDate");

                entity.Property(e => e.MetaDataId).HasColumnName("MetaDataID");

                entity.Property(e => e.Context).HasDefaultValueSql("(newid())");

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Error)
                    .HasMaxLength(4000)
                    .IsUnicode(false);

                entity.Property(e => e.Result)
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.RowTimeStamp)
                    .IsRequired()
                    .IsRowVersion();
            });

            modelBuilder.Entity<OnDemandTask>(entity =>
            {
                entity.ToTable("OnDemandTask", "PPS");

                entity.Property(e => e.OnDemandTaskId)
                    .HasColumnName("OnDemandTaskID")
                    .ValueGeneratedNever();

                entity.Property(e => e.Active)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.Description)
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.ExecutorId).HasColumnName("ExecutorID");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.RowTimeStamp)
                    .IsRequired()
                    .IsRowVersion();

                entity.Property(e => e.SettingId).HasColumnName("SettingID");

                entity.Property(e => e.SheduleId).HasColumnName("SheduleID");

                entity.Property(e => e.SssbserviceName)
                    .HasColumnName("SSSBServiceName")
                    .HasMaxLength(128)
                    .IsUnicode(false);

                entity.HasOne(d => d.Executor)
                    .WithMany(p => p.OnDemandTask)
                    .HasForeignKey(d => d.ExecutorId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_OnDemandTask_Executor");

                entity.HasOne(d => d.Setting)
                    .WithMany(p => p.OnDemandTask)
                    .HasForeignKey(d => d.SettingId)
                    .HasConstraintName("FK_OnDemandTask_Setting");

                entity.HasOne(d => d.Shedule)
                    .WithMany(p => p.OnDemandTask)
                    .HasForeignKey(d => d.SheduleId)
                    .HasConstraintName("FK_OnDemandTask_Shedule");
            });

            modelBuilder.Entity<Setting>(entity =>
            {
                entity.ToTable("Setting", "PPS");

                entity.Property(e => e.SettingId)
                    .HasColumnName("SettingID")
                    .ValueGeneratedNever();

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.Description)
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.RowTimeStamp)
                    .IsRequired()
                    .IsRowVersion();

                entity.Property(e => e.Settings)
                    .IsRequired()
                    .HasColumnType("xml");
            });

            modelBuilder.Entity<Shedule>(entity =>
            {
                entity.ToTable("Shedule", "PPS");

                entity.HasIndex(e => e.Name)
                    .HasName("UK_Shedule_Name")
                    .IsUnique();

                entity.Property(e => e.SheduleId)
                    .HasColumnName("SheduleID")
                    .ValueGeneratedNever();

                entity.Property(e => e.Active)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.RowTimeStamp)
                    .IsRequired()
                    .IsRowVersion();
            });
        }
    }
}
