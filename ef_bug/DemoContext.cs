using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ef_bug
{
    internal class DemoContext : DbContext
    {
        public DemoContext()
        {
            
        }

        public DemoContext(DbContextOptions<DemoContext> options)
            : base(options)
        {
        }



		public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderItem> OrderItems { get; set; }


		protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Order>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId);
            });
            modelBuilder.Entity<OrderItem>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Type).HasConversion<string>();

            });


        }
    }
}
