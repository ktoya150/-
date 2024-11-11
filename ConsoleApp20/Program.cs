using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TaxiFleetManagement
{
    public class OrderEventArgs : EventArgs
    {
        public Order Order { get; set; }
    }

    public class Order
    {
        public string ClientName { get; set; }
        public string Destination { get; set; }
        public bool IsVip { get; set; }
        public bool IsAssigned { get; set; }

        public Order(string clientName, string destination, bool isVip = false)
        {
            ClientName = clientName;
            Destination = destination;
            IsVip = isVip;
            IsAssigned = false;
        }
    }

    public class Driver
    {
        public string Name { get; set; }
        public bool IsAvailable { get; set; } = true;

        public event EventHandler<OrderEventArgs> OrderAssigned;

        public Driver(string name)
        {
            Name = name;
        }

        public void AssignOrder(Order order)
        {
            IsAvailable = false;
            Console.WriteLine($"Водій {Name} отримав нове замовлення від {order.ClientName} на поїздку до {order.Destination}");
            OrderAssigned?.Invoke(this, new OrderEventArgs { Order = order });
            CompleteOrder(order);
        }

        public void CompleteOrder(Order order)
        {
            Console.WriteLine($"Водій {Name} завершив поїздку для {order.ClientName}");
            IsAvailable = true;
            order.IsAssigned = true;
            UnsubscribeEvents();
        }

        private void UnsubscribeEvents()
        {
            OrderAssigned = null;
        }
    }

    public class TaxiService
    {
        private List<Driver> _drivers = new List<Driver>();
        private Queue<Order> _orderQueue = new Queue<Order>();

        public event EventHandler<OrderEventArgs> OrderCreated;

        public void AddDriver(Driver driver)
        {
            _drivers.Add(driver);
        }

        public void CreateOrder(string clientName, string destination, bool isVip = false)
        {
            var order = new Order(clientName, destination, isVip);
            Console.WriteLine($"Нове замовлення: {clientName} викликає таксі до {destination} (VIP: {isVip})");
            _orderQueue.Enqueue(order);
            OrderCreated?.Invoke(this, new OrderEventArgs { Order = order });
            AssignOrders();
        }

        private void AssignOrders()
        {
            var freeDrivers = _drivers.Where(d => d.IsAvailable).ToList();

            while (_orderQueue.Count > 0 && freeDrivers.Count > 0)
            {
                var order = _orderQueue
                    .OrderByDescending(o => o.IsVip)
                    .FirstOrDefault(o => !o.IsAssigned);

                if (order == null) break;

                var driver = freeDrivers.FirstOrDefault();
                driver?.AssignOrder(order);
                _orderQueue = new Queue<Order>(_orderQueue.Where(o => !o.IsAssigned));
                freeDrivers = _drivers.Where(d => d.IsAvailable).ToList();
            }
        }

        public void Run()
        {
            string command;
            do
            {
                Console.WriteLine("\n1. Створити замовлення\n2. Показати статус водіїв\n3. Завершити програму");
                command = Console.ReadLine();

                switch (command)
                {
                    case "1":
                        Console.WriteLine("Ім'я клієнта:");
                        string clientName = Console.ReadLine();
                        Console.WriteLine("Місце призначення:");
                        string destination = Console.ReadLine();
                        Console.WriteLine("VIP-клієнт? (y/n):");
                        bool isVip = Console.ReadLine().ToLower() == "y";

                        CreateOrder(clientName, destination, isVip);
                        break;

                    case "2":
                        ShowDriverStatus();
                        break;

                    case "3":
                        Console.WriteLine("Завершення роботи системи.");
                        break;

                    default:
                        Console.WriteLine("Невідома команда.");
                        break;
                }
            } while (command != "3");
        }

        private void ShowDriverStatus()
        {
            foreach (var driver in _drivers)
            {
                Console.WriteLine($"Водій {driver.Name} - {(driver.IsAvailable ? "Вільний" : "Зайнятий")}");
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Установка UTF-8 для корректного отображения кириллицы
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            TaxiService taxiService = new TaxiService();

            Driver driver1 = new Driver("Петро");
            Driver driver2 = new Driver("Андрій");

            taxiService.AddDriver(driver1);
            taxiService.AddDriver(driver2);

            taxiService.OrderCreated += (sender, e) =>
            {
                Console.WriteLine($"Клієнт {e.Order.ClientName} отримає таксі до {e.Order.Destination}");
            };

            taxiService.Run();
        }
    }
}
