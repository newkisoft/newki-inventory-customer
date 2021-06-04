using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using newkilibraries;
namespace Web
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<AgentCustomer>().HasKey(sc => new { sc.CustomerId, sc.AgentId });            
            builder.Entity<CustomerDataView>().HasKey(sc=>sc.CustomerId);
        }
        public DbSet<Customer> Customer { get; set; }
        public DbSet<CustomerDataView> CustomerDataView { get; set; }
    }
}