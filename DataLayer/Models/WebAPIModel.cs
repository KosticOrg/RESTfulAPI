using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Models
{
    public class WebAPIModel : IdentityDbContext
    {
        public WebAPIModel()
        {

        }
        public WebAPIModel(DbContextOptions<WebAPIModel> options)
            :base(options)
        {

        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseSqlServer("Server=tcp:xxxx.database.windows.net,1433;Initial Catalog=xxxx;Persist Security Info=False;User ID=xxxx;Password=xxxxxx;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
        }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("dbo");
            modelBuilder.Entity<Address>(entity =>
            {
                entity.ToTable("Address");

                entity.OwnsOne(e => e.AdditionalInfo,
                    a =>
                    {
                        a.Property(p => p.StreetNumber)
                             .HasMaxLength(10)
                             .HasColumnName("StreetNumber");
                        a.Property(p => p.AdditionalNumber)
                             .HasMaxLength(10)
                             .HasColumnName("AdditionalNumber");
                        a.Property(p => p.Zip)
                             .HasMaxLength(10)
                             .HasColumnName("ZipCode");

                    });

                entity.Property(e => e.Street)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.City)
                      .IsRequired()
                      .HasMaxLength(15);

                entity.Property(e => e.Country)
                      .IsRequired()
                      .HasMaxLength(20);
            });
            modelBuilder.Entity<Student>(entity =>
            {
                entity.ToTable("Student");

                entity.HasIndex(e => e.StudentNumber)
                      .IsUnique();

                entity.Property(e => e.FirstName)
                      .IsRequired()
                      .HasMaxLength(15);

                entity.Property(e => e.LastName)
                      .IsRequired()
                      .HasMaxLength(15);

                entity.Property(e => e.StudentNumber)
                      .IsRequired()
                      .IsConcurrencyToken();

                entity.Property(e => e.CreatedBy)
                      .IsRequired()
                      .HasMaxLength(60);

                entity.Property(e => e.Avatar)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasOne(d => d.Address)
                      .WithMany(p => p.Students)
                      .HasForeignKey(e => e.AddressId)
                      .OnDelete(DeleteBehavior.NoAction);

            });
            modelBuilder.Entity<Course>(entity =>
            {
                // Entity Configuration

                entity.ToTable("Course");

                // Property Configuration

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(e => e.Teacher)
                       .IsRequired()
                       .HasMaxLength(30);

            });          
            base.OnModelCreating(modelBuilder);
        }
    }
}
