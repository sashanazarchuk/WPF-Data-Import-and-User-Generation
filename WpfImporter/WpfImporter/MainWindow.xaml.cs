using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Win32;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;


namespace WpfImporter
{
    public partial class MainWindow : Window
    {
        //PostgreSQL connection string.
        string connectionString = "Server=localhost;Port=5432;Database=WpfTest;User Id=postgres;Password=21225807;";

        // Initialize a stopwatch to measure import time.
        private Stopwatch importTimer = new Stopwatch();

        // Create a HashSet to keep track of imported customer IDs.
        private HashSet<int> importedCustomerIds = new HashSet<int>();


        public MainWindow()
        {
            InitializeComponent();
        }


        //Button for Import data
        private async void ImportDataButton_Click(object sender, RoutedEventArgs e)
        {
            // Execute the import operation asynchronously.
            await Task.Run(() => ImportDataFromCSV());
        }


        // Method to import data from a CSV file.
        private async Task ImportDataFromCSV()
        {

            // Create a file dialog to select the CSV file to import.
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Select a CSV file to import"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Get the selected CSV file path and other import settings.
                string csvFilePath = openFileDialog.FileName;
                string tableName = "\"Customer\"";
                int batchSize = 1000;

                // Open a connection to the PostgreSQL database.
                using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Read data from the selected CSV file using CsvHelper.
                    using (var reader = new StreamReader(csvFilePath))
                    using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                    {

                        // Read records from the CSV file and store them in a list.
                        var records = csv.GetRecords<Customer>().ToList();
                        int totalRecords = records.Count;

                        // Create a binary importer to efficiently copy data to the database.
                        using (var writer = connection.BeginBinaryImport($"COPY {tableName} (\"Id\", \"Name\", \"Age\", \"Email\", \"Phone\", \"BirthDate\", \"Country\", \"Company\") FROM STDIN BINARY"))
                        {
                            var batchList = new List<Customer>(batchSize);
                            int importedCount = 0;

                            // Start measuring the import time.
                            importTimer.Start();

                            foreach (var customer in records)
                            {
                                batchList.Add(customer);

                                // If the batch size is reached, write the batch to the database.
                                if (batchList.Count >= batchSize)
                                {
                                    await WriteBatchAsync(writer, batchList);
                                    batchList.Clear();
                                    importedCount += batchSize;
                                }
                            }


                            // Write any remaining records in the last batch.
                            if (batchList.Count > 0)
                            {
                                await WriteBatchAsync(writer, batchList);
                                importedCount += batchList.Count;
                            }

                            // Complete the binary import.
                            writer.Complete();

                            // Stop measuring the import time.
                            importTimer.Stop();

                            TimeSpan elapsedTime = importTimer.Elapsed;

                            if (importedCount == totalRecords)
                            {
                                // Show a completion message if all records are imported.
                                MessageBox.Show($"Import complete. Import time: {((int)elapsedTime.TotalMinutes)} minutes and {((int)elapsedTime.TotalSeconds)} seconds");
                            }
                        }
                    }
                }
            }
        }

        // Method to write a batch of data to the database asynchronously
        private async Task WriteBatchAsync(NpgsqlBinaryImporter writer, List<Customer> batch)
        {
            // Open a connection to the PostgreSQL database
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Start a transaction to group the batch write operation.
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var customer in batch)
                    {
                        // Skip customers that have already been imported.
                        if (importedCustomerIds.Contains(customer.Id))
                        {
                            continue;
                        }
                        // Use an upsert (INSERT ON CONFLICT) command to insert or update the data.
                        using (var upsertCommand = new NpgsqlCommand($"INSERT INTO \"Customer\" (\"Id\", \"Name\", \"Age\", \"Email\", \"Phone\", \"BirthDate\", \"Country\", \"Company\") " +
                            "VALUES (@Id, @Name, @Age, @Email, @Phone, @BirthDate, @Country, @Company) " +
                            "ON CONFLICT (\"Id\") DO UPDATE " +
                            "SET \"Name\" = EXCLUDED.\"Name\", \"Age\" = EXCLUDED.\"Age\", \"Email\" = EXCLUDED.\"Email\", " +
                            "\"Phone\" = EXCLUDED.\"Phone\", \"BirthDate\" = EXCLUDED.\"BirthDate\", \"Country\" = EXCLUDED.\"Country\", " +
                            "\"Company\" = EXCLUDED.\"Company\"", connection, transaction))
                        {
                            upsertCommand.Parameters.AddWithValue("Id", customer.Id);
                            upsertCommand.Parameters.AddWithValue("Name", customer.Name);
                            upsertCommand.Parameters.AddWithValue("Age", customer.Age);
                            upsertCommand.Parameters.AddWithValue("Email", customer.Email);
                            upsertCommand.Parameters.AddWithValue("Phone", customer.Phone);
                            upsertCommand.Parameters.AddWithValue("BirthDate", customer.BirthDate);
                            upsertCommand.Parameters.AddWithValue("Country", customer.Country);
                            upsertCommand.Parameters.AddWithValue("Company", customer.Company);

                            // Execute the upsert command asynchronously.
                            await upsertCommand.ExecuteNonQueryAsync();

                            // Add the imported ID to the HashSet to avoid re-importing.
                            importedCustomerIds.Add(customer.Id);
                        }
                    }
                    // Commit the transaction after processing the batch.
                    transaction.Commit();
                }
            }
        }


        //The Customer class to represent the data structure.
        public class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public DateTime BirthDate { get; set; }
            public string Country { get; set; }
            public string Company { get; set; }
        }
    }
}