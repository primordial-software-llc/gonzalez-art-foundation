﻿using System;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using GalleryBackend;
using GalleryBackend.Model;
using IndexBackend;
using IndexBackend.Indexing;
using IndexBackend.NationalGalleryOfArt;

namespace SlideshowIndexer
{
    class Program
    {
        private static readonly PrivateConfig PrivateConfig = PrivateConfig.CreateFromPersonalJson();

        private static readonly IAmazonS3 S3Client = new AwsClientFactory().CreateS3Client();
        private static readonly IAmazonDynamoDB DynamoDbClient = new AwsClientFactory().CreateDynamoDbClient();

        static void Main(string[] args)
        {
            var galleryClient = new GalleryClient(
                PrivateConfig.GalleryUsername,
                PrivateConfig.GalleryPassword);
            var vpnCheck = new VpnCheck(galleryClient);
            var vpnInUse = vpnCheck.IsVpnInUse(PrivateConfig.DecryptedIp);

            if (!string.IsNullOrWhiteSpace(vpnInUse))
            {
                Console.WriteLine(vpnInUse);
                return;
            }
            Console.WriteLine("VPN is in use with IP: " + vpnInUse);

            Console.WriteLine("Backing up DynamoDb Data to S3");
            var request = new CreateBackupRequest
            {
                TableName = new ClassificationModel().GetTable(),
                BackupName = "image-classification-backup-" + DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ssZ")
            };
            var backupResponse = DynamoDbClient.CreateBackup(request);
            Console.WriteLine("Backup started: " + backupResponse.BackupDetails.BackupStatus + " - " + backupResponse.BackupDetails.BackupArn);

            IIndex indexer = GetIndexer(IndexType.NationalGalleryOfArt);
            var fileIdQueueIndexer = new FileIdQueueIndexer();

            try
            {
                fileIdQueueIndexer.Index(indexer);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        enum IndexType
        {
            NationalGalleryOfArt,
            TheAthenaeum
        }

        private static IIndex GetIndexer(IndexType indexType)
        {
            if (indexType == IndexType.NationalGalleryOfArt)
            {
                var ngaDataAccess = new NationalGalleryOfArtDataAccess(PublicConfig.NationalGalleryOfArtUri);
                ngaDataAccess.Init();
                var indexer = new NationalGalleryOfArtIndexer(S3Client, DynamoDbClient, ngaDataAccess);
                return indexer;
            }
            else if (indexType == IndexType.TheAthenaeum)
            {
                return new TheAthenaeumIndexer(
                    PrivateConfig.PageNotFoundIndicatorText,
                    DynamoDbClient,
                    PublicConfig.TheAthenaeumArt);
            }
            else
            {
                return null;
            }
        }

    }
}
