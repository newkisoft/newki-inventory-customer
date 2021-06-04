using System.Collections.Generic;
using System.Linq;
using Amazon.SQS;
using newkilibraries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Net.Http;
using System.Text;
using Amazon;
using System;
using newki_inventory_customer.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading;

namespace newki_inventory_customer
{
    class Program
    {
        static ManualResetEvent _quitEvent = new ManualResetEvent(false);
        private static string _connectionString;

        static void Main(string[] args)
        {
            //Reading configuration
            var customers = new List<Customer>();
            var awsStorageConfig = new AwsStorageConfig();
            var builder = new ConfigurationBuilder()
            .AddJsonFile($"appsettings.json", true, true);
            var Configuration = builder.Build();

            Configuration.GetSection("AwsStorageConfig").Bind(awsStorageConfig);


            var services = new ServiceCollection();

            _connectionString = Configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_connectionString));
            services.AddTransient<IAwsService, AwsService>();
            services.AddTransient<ICustomerService, CustomerService>();
            services.AddTransient<IRabbitMqService, RabbitMqService>();
            services.AddSingleton<IAwsStorageConfig>(awsStorageConfig);

            var serviceProvider = services.BuildServiceProvider();
            var rabbitMq = serviceProvider.GetService<IRabbitMqService>();

            InventoryMessage inventoryMessage;

            ConnectionFactory factory = new ConnectionFactory();
            factory.UserName = "user";
            factory.Password = "password";
            factory.HostName = "localhost";

            var connection = factory.CreateConnection();

            var requestQueueName = "CustomerRequest";
            var responseQueueName = "CustomerResponse";

            var channel = connection.CreateModel();
            channel.QueueDeclare(requestQueueName, false, false, false);
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (ch, ea) =>
            {
                var content = Encoding.UTF8.GetString(ea.Body.ToArray());
                var updateCustomerFullNameModel = JsonSerializer.Deserialize<InventoryMessage>(content);

                ProcessRequest(updateCustomerFullNameModel);

            }; ;
            channel.BasicConsume(queue: requestQueueName,
                   autoAck: true,
                   consumer: consumer);


            _quitEvent.WaitOne();

        }

        private static void ProcessRequest(InventoryMessage inventoryMessage)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseNpgsql(_connectionString);

                using (var appDbContext = new ApplicationDbContext(optionsBuilder.Options))
                {
                    var customerService = new CustomerService(appDbContext);

                    var messageType = Enum.Parse<InventoryMessageType>(inventoryMessage.Command);

                    switch (messageType)
                    {
                        case InventoryMessageType.Search:
                            {
                                ProcessSearch(customerService, appDbContext);
                                break;
                            }
                        case InventoryMessageType.Get:
                            {
                                Console.WriteLine("Loading a Customer...");
                                var id = JsonSerializer.Deserialize<int>(inventoryMessage.Message);
                                var customer = customerService.GetCustomer(id);
                                var content = JsonSerializer.Serialize(customer);

                                var responseMessageNotification = new InventoryMessage();
                                responseMessageNotification.Command = InventoryMessageType.Get.ToString();
                                responseMessageNotification.RequestNumber = inventoryMessage.RequestNumber;
                                responseMessageNotification.MessageDate = DateTimeOffset.UtcNow;

                                var inventoryResponseMessage = new InventoryMessage();
                                inventoryResponseMessage.Message = content;
                                inventoryResponseMessage.Command = inventoryMessage.Command;
                                inventoryResponseMessage.RequestNumber = inventoryMessage.RequestNumber;

                                Console.WriteLine("Sending the message back");

                                break;

                            }
                        case InventoryMessageType.Insert:
                            {
                                Console.WriteLine("Adding new Customer");
                                var customer = JsonSerializer.Deserialize<Customer>(inventoryMessage.Message);
                                customerService.Insert(customer);
                                ProcessSearch(customerService, appDbContext);
                                break;
                            }
                        case InventoryMessageType.Update:
                            {
                                Console.WriteLine("Updating a Customer");
                                var Customer = JsonSerializer.Deserialize<Customer>(inventoryMessage.Message);
                                customerService.Update(Customer);
                                var existingCustomer = appDbContext.CustomerDataView.Find(Customer.CustomerId);
                                existingCustomer.Data = JsonSerializer.Serialize(Customer);
                                appDbContext.SaveChanges();
                                break;
                            }
                        case InventoryMessageType.Delete:
                            {
                                Console.WriteLine("Deleting a Customer");
                                var id = JsonSerializer.Deserialize<int>(inventoryMessage.Message);
                                customerService.Remove(id);
                                var removeCustomer = appDbContext.CustomerDataView.FirstOrDefault(predicate => predicate.CustomerId == id);
                                appDbContext.CustomerDataView.Remove(removeCustomer);
                                appDbContext.SaveChanges();
                                break;
                            }
                        default: break;

                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void ProcessSearch(ICustomerService customerService, ApplicationDbContext appDbContext)
        {
            Console.WriteLine("Loading all the customers...");
            var customers = customerService.GetCustomers();

            foreach (var customer in customers)
            {
                if (appDbContext.CustomerDataView.Any(p => p.CustomerId == customer.CustomerId))
                {
                    var existingCustomer = appDbContext.CustomerDataView.Find(customer.CustomerId);
                    existingCustomer.Data = JsonSerializer.Serialize(customer);
                }
                else
                {
                    var CustomerDataView = new CustomerDataView
                    {
                        CustomerId = customer.CustomerId,
                        Data = JsonSerializer.Serialize(customer)
                    };
                    appDbContext.CustomerDataView.Add(CustomerDataView);
                }
                appDbContext.SaveChanges();
            }
        }
    }
}
