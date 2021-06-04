using System.Collections.Generic;
using System.Linq;
using newkilibraries;

namespace newki_inventory_customer
{
    public interface ICustomerService
    {
        List<Customer> GetCustomers();

        Customer GetCustomer(int id);

        void Insert(Customer customer);

        void Update(Customer customer);

        void Remove(int id);
 
    }
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;

        public CustomerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Customer> GetCustomers()
        {
            return _context.Customer.OrderByDescending(p=>p.CustomerId).ToList();
        }

        public Customer GetCustomer(int id)
        {  
            return _context.Customer.FirstOrDefault(p=>p.CustomerId == id);                
        } 

        public void Insert(Customer customer)
        {            
            _context.Customer.Add(customer);
            _context.SaveChanges();
        }

        public void Update(Customer customer)
        {            
            var existingCustomer = _context.Customer.Find(customer.CustomerId);
            _context.Entry<Customer>(existingCustomer).CurrentValues.SetValues(customer);
            _context.SaveChanges();
        }

        public void Remove(int id)
        {            
            var customer = _context.Customer
                .Where(x => x.CustomerId == id)
                .FirstOrDefault();
            _context.Customer.Remove(customer);
            _context.SaveChanges();
        }
    }
}