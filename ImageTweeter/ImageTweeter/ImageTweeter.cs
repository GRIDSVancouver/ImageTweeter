using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Linq;

namespace ImageTweeter
{
    public static class ImageTweeter
    {
        [FunctionName("ImageTweeter")]
        //original every-5-min cron: "0 */5 * * * *"
        public static void Run([TimerTrigger("*/10 * * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            //todo these should prolly be in config
            const string UnprocessedContainerName = "unprocessedimages";
            const string ProcessedContainerName = "processedimages";

            //grab file

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer unprocessedContainer = blobClient.GetContainerReference(UnprocessedContainerName);
            //todo: handle non-CloudBlobs gracefully
            var sourceBlob = (CloudBlockBlob) unprocessedContainer.ListBlobs().First();

            log.Info($"About to Tweet blob '{sourceBlob.Name}'");

            //tweet



            //move file
            CloudBlobContainer processedContainer = blobClient.GetContainerReference(ProcessedContainerName);
            var destinationBlob = processedContainer.GetBlockBlobReference(sourceBlob.Name);
            destinationBlob.StartCopy(sourceBlob);
            //seems sketchy to not confirm that the copy has finished before deleting, but some StackOverflow rando said it's OK!
            sourceBlob.Delete(DeleteSnapshotsOption.IncludeSnapshots);
            log.Info($"Moved file to '{ProcessedContainerName}'");
        }
    }
}
