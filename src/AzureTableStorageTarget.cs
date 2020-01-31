using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using NLog.Common;
using NLog.Config;
using NLog.Targets;

namespace NLog.Extensions.AzureTable {
    /// <summary>
    /// AzureTableStorageTarget
    /// </summary>
    [Target("AzureTable")]
    public class AzureTableStorageTarget : AsyncTaskTarget {
        // Fields
        private string _connectionString;
        private string _tableName;
        // Properties
        private CloudTable Table { get; set; }
        [RequiredParameter]
        public string ConnectionString {
            get => _connectionString;
            set {
                _connectionString = value;
                Table = InitializeTable(_connectionString, TableName);
            }
        }
        public string TableName {
            get => _tableName; set {
                _tableName = value;
                Table = InitializeTable(_connectionString, TableName);
            }
        }
        // Methods
        protected override void InitializeTarget() {
            base.InitializeTarget();
            if (string.IsNullOrEmpty(TableName)) {
                string tableName = Process.GetCurrentProcess().ProcessName;
                tableName = Path.GetFileNameWithoutExtension(tableName);
                if (string.Compare(tableName, "dotnet", true) == 0) {
                    string[] args = Environment.GetCommandLineArgs();
                    if (args != null && args.Length > 0) {
                        tableName = args[0];
                        tableName = Path.GetFileNameWithoutExtension(tableName);
                    }
                }
                tableName = ReformatTableName(tableName);
                ValidateTableName(tableName);
                TableName = tableName;
            }
        }
        protected override Task WriteAsyncTask(LogEventInfo logEvent, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }
        protected override async Task WriteAsyncTask(IList<LogEventInfo> logEvents, CancellationToken cancellationToken) {
            try {
                TableBatchOperation batch = new TableBatchOperation();
                foreach (var e in logEvents)
                    batch.Insert(CreateTableEntity(e));
                await Table.ExecuteBatchAsync(batch);
            }
            catch (Exception exception) {
                InternalLogger.Error(exception, "Error writing to azure storage table: {exception}", exception.Message);
            }
        }
        private ITableEntity CreateTableEntity(LogEventInfo logEvent) {
            DynamicTableEntity entity = new DynamicTableEntity();
            foreach (var contextproperty in ContextProperties) {
                if (string.IsNullOrEmpty(contextproperty.Name))
                    continue;
                var propertyValue = contextproperty.Layout != null ? RenderLogEvent(contextproperty.Layout, logEvent) : string.Empty;
                if (nameof(entity.PartitionKey) == contextproperty.Name) {
                    entity.PartitionKey = propertyValue;
                    continue;
                } else if (nameof(entity.RowKey) == contextproperty.Name) {
                    entity.RowKey = propertyValue;
                    continue;
                } else if (nameof(entity.ETag) == contextproperty.Name) {
                    entity.ETag = propertyValue;
                    continue;
                }
                entity.Properties.Add(contextproperty.Name, new EntityProperty(propertyValue));
            }
            return entity;
        }
        // Statics
        private static List<string> _reservedWords = new List<string> { "tables" };
        private static void ValidateTableName(string tableName) {
            var valid = !_reservedWords.Contains(tableName) && Regex.IsMatch(tableName, @"^[A-Za-z][A-Za-z0-9]{2,62}$");
            if (!valid) {
                throw new NotSupportedException(tableName + " is not a valid name for Azure storage table name.") {
                    HelpLink = "http://msdn.microsoft.com/en-us/library/windowsazure/dd179338.aspx"
                };
            }
        }
        private static string ReformatTableName(string name) {
            // https://blogs.msdn.microsoft.com/jmstall/2014/06/12/azure-storage-naming-rules/
            string tbName = Regex.Replace(name, @"[^\da-z]*", string.Empty, RegexOptions.IgnoreCase);
            if (string.IsNullOrWhiteSpace(name) || name.Length < 3)
                return "defaultlogstable";
            if (tbName.Length > 63)
                return tbName.Substring(0, 63);
            return tbName;
        }
        private static CloudTable InitializeTable(string connectionString, string tableName) {
            CloudTable table = null;
            if (string.IsNullOrWhiteSpace(connectionString)
                || string.IsNullOrWhiteSpace(tableName)) {
                return table;
            }

            try {
                // get the storage account
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
                //storageAccount.TableEndpoint
                System.Net.ServicePoint tableServicePoint = System.Net.ServicePointManager.FindServicePoint(storageAccount.TableEndpoint);
                tableServicePoint.UseNagleAlgorithm = false;
                // Create the table client.
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                //create charts table if not exists.
                table = tableClient.GetTableReference(tableName);
                table.CreateIfNotExists();
            }
            catch (Exception exception) {
                // write to nlog's internal logger (if configured)
                InternalLogger.Error(exception, "Exception in AzureTableStorage initialization:{0}", exception.Message);
            }
            return table;
        }
    }
}
