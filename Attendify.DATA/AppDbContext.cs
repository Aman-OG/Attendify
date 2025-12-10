
using Attendify.DATA.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendify.DATA
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // TABLES
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Attendance> Attendance { get; set; }
        public DbSet<Leave> Leaves { get; set; }
        public DbSet<AttendanceRule> AttendanceRules { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<AttendancePerShift> AttendancePerShift { get; set; }
        public DbSet<AdminMessage> AdminMessages { get; set; }
        public DbSet<EmployeeRequest> EmployeeRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // EMPLOYEE → ATTENDANCE (1:M)
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Employee)
                .WithMany(e => e.Attendance)
                .HasForeignKey(a => a.EmpCode)
                .OnDelete(DeleteBehavior.Cascade);

            // EMPLOYEE → LEAVES (1:M)
            modelBuilder.Entity<Leave>()
                .HasOne(l => l.Employee)
                .WithMany(e => e.Leaves)
                .HasForeignKey(l => l.EmpCode)
                .OnDelete(DeleteBehavior.Cascade);

            // EMPLOYEE → REQUESTS (1:M)
            modelBuilder.Entity<EmployeeRequest>()
                .HasOne(r => r.Employee)
                .WithMany(e => e.EmployeeRequests)
                .HasForeignKey(r => r.EmpCode)
                .OnDelete(DeleteBehavior.Cascade);

            // ATTENDANCE → AttendancePerShift (1:M)
            modelBuilder.Entity<AttendancePerShift>()
                .HasOne(ap => ap.Attendance)
                .WithMany(a => a.AttendancePerShifts)
                .HasForeignKey(ap => ap.AttendanceID)   // Assuming this one is unchanged
                .OnDelete(DeleteBehavior.Cascade);

            // SHIFT → AttendancePerShift (1:M)
            modelBuilder.Entity<AttendancePerShift>()
                .HasOne(ap => ap.Shift)
                .WithMany(s => s.AttendancePerShifts)
                .HasForeignKey(ap => ap.ShiftID)
                .OnDelete(DeleteBehavior.Cascade);

            // UNIQUE Email (Employees)
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Email)
                .IsUnique();
        }

    }
}
