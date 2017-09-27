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
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            //grab file

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("unprocessedimages");
            //todo: pick at random?
            var image = (CloudBlob) container.ListBlobs().First();

            log.Info($"About to execute on blob '{image.Name}'");

            //tweet

            //move file
        }


        static void MoveBlobInSameStorageAccount(CloudStorageAccount storageAccount)
        {
            var client = storageAccount.CreateCloudBlobClient();
            var sourceContainer = client.GetContainerReference("source-container-name");
            var sourceBlob = sourceContainer.GetBlockBlobReference("blob-name");
            var destinationContainer = client.GetContainerReference("destination-container-name");
            var destinationBlob = destinationContainer.GetBlockBlobReference("blob-name");
            destinationBlob.StartCopy(sourceBlob);
            sourceBlob.Delete(DeleteSnapshotsOption.IncludeSnapshots);
        }
    }
}
