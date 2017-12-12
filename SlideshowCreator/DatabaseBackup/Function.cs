using System;
using System.Collections.Generic;
using System.Linq;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace DatabaseBackup
{
    public class Function
    {
        private IAmazonDynamoDB Client { get; }

        public Function()
            : this(new AmazonDynamoDBClient(new AmazonDynamoDBConfig { RegionEndpoint = RegionEndpoint.USEast1 }))
        {
        }

        public Function(IAmazonDynamoDB client)
        {
            Client = client;
        }

        public string FunctionHandler(ILambdaContext context)
        {
            ListTablesResponse response;
            List<string> allTableNames = new List<string>();
            do
            {
                response = Client.ListTablesAsync(new ListTablesRequest
                {
                    ExclusiveStartTableName = allTableNames.LastOrDefault()
                }).Result;
                allTableNames.AddRange(response.TableNames);
            } while (!string.IsNullOrWhiteSpace(response.LastEvaluatedTableName));
            List<BackupDetails> backupResponses = new List<BackupDetails>();
            foreach (var table in allTableNames)
            {
                var backupResponse = StartBackup(table);
                backupResponses.Add(backupResponse);
                Console.WriteLine($"{backupResponse.BackupStatus}: {backupResponse.BackupArn}");
            }
            return string.Join(
                ", ",
                backupResponses
                    .Select(backupResponse => $"{backupResponse.BackupStatus}: {backupResponse.BackupArn}"));
        }

        public BackupDetails StartBackup(string table)
        {
            var request = new CreateBackupRequest
            {
                TableName = table,
                BackupName = $"{table}-backup-{DateTime.UtcNow:yyyy-MM-ddTHH-mm-ssZ}"
            };
            var backupResponse = Client.CreateBackupAsync(request).Result;
            return backupResponse.BackupDetails;
        }
        
    }
}
