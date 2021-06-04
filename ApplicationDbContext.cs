using Microsoft.EntityFrameworkCore;
using newkilibraries;

namespace newki_inventory_customer
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<AgentCustomer>().HasKey(sc => new { sc.CustomerId, sc.AgentId });            
            builder.Entity<Customer>().HasKey(sc => new { sc.CustomerId});            
            builder.Entity<CustomerDataView>().HasKey(sc => new { sc.CustomerId});            
        }
        public DbSet<Customer> Customer { get; set; }
        public DbSet<CustomerDataView> CustomerDataView { get; set; }
    }
}