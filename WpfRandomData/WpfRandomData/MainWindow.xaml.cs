using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfRandomData
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Random random = new Random();
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Record_Data(object sender, RoutedEventArgs e)
        {
            int numUsers = 70000;

            // Configure the OpenFileDialog to select a file location
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select a File",
                CheckFileExists = true,
                CheckPathExists = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                // Create a list of users
                List<User> users = GenerateUsers(numUsers);

                await WriteUsersToCsvAsync(users, filePath);

                MessageBox.Show($"Generated and saved {numUsers} users to the file {filePath}");
            }
        }

        private List<User> GenerateUsers(int numUsers)
        {
            List<User> users = new List<User>();

            for (int i = 0; i < numUsers; i++)
            {
                User user = new User
                {
                    Id = i + 1,
                    Name = GenerateRandomName(),
                    Age = random.Next(18, 70),
                    Email = GenerateRandomEmail(),
                    Phone = GenerateRandomPhone(),
                    Birthday = DateTime.Now.AddYears(-random.Next(18, 70)),
                    Country = GenerateRandomCountry(),
                    Company = GenerateRandomCompany()
                };
                users.Add(user);
            }

            return users;
        }

        private string GenerateRandomName()
        {
            string[] firstNames = { "John", "Alice", "Bob", "Emily", "Michael", "Olivia", "David", "Sophia" };
            string[] lastNames = { "Smith", "Johnson", "Brown", "Taylor", "Williams", "Jones", "Davis", "Wilson" };
            string firstName = firstNames[random.Next(firstNames.Length)];
            string lastName = lastNames[random.Next(lastNames.Length)];
            return $"{firstName} {lastName}";
        }

        private string GenerateRandomCompany()
        {
            string[] companies = { "ABC Inc.", "XYZ Corporation", "Tech Solutions Ltd.", "Global Innovations Co.", "Best Services Group" };
            return companies[random.Next(companies.Length)];
        }

        private string GenerateRandomEmail()
        {
            string[] domains = { "gmail.com", "yahoo.com", "outlook.com", "example.com", "mail.com" };

            string name = GenerateRandomName().Replace(" ", ""); // You can use the generated name
            int number = random.Next(10, 99); // You can choose the number range you need
            string domain = domains[random.Next(domains.Length)];
            return $"{name}{number}@{domain}";
        }

        private string GenerateRandomPhone()
        {
            string countryCode = "+1";
            string regionCode = random.Next(100, 999).ToString();
            string phoneNumber = random.Next(1000, 9999).ToString();
            return $"{countryCode}-{regionCode}-{phoneNumber}";
        }

        private string GenerateRandomCountry()
        {
            string[] countries = { "Ukraine", "Poland", "Japan", "Spain", "Germany" };
            return countries[random.Next(countries.Length)];
        }

        private async Task WriteUsersToCsvAsync(List<User> users, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                // Write the header
                await writer.WriteLineAsync("Id,Name,Age,Email,Phone,Birthday,Country,Company");

                // Write user data
                foreach (var user in users)
                {
                    await writer.WriteLineAsync($"{user.Id},{user.Name},{user.Age},{user.Email},{user.Phone},{user.Birthday:yyyy-MM-dd},{user.Country},{user.Company}");
                }
            }
        }

        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public DateTime Birthday { get; set; }
            public string Country { get; set; }
            public string Company { get; set; }
        }
    }
}
