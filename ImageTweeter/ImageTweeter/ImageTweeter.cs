using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Linq;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using System.Collections.Generic;

namespace ImageTweeter
{
    public static class ImageTweeter
    {
        [FunctionName("ImageTweeter")]
        public static void Run([TimerTrigger("0 20 10 * * *")]TimerInfo myTimer, TraceWriter log)
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

            var consumerKey = CloudConfigurationManager.GetSetting("TwitterConsumerKey");
            var consumerSecret = CloudConfigurationManager.GetSetting("TwitterConsumerSecret");
            var accessToken = CloudConfigurationManager.GetSetting("TwitterAccessToken");
            var accessTokenSecret = CloudConfigurationManager.GetSetting("TwitterAccessTokenSecret");
            
            ExceptionHandler.SwallowWebExceptions = false;
            Auth.SetUserCredentials(consumerKey, consumerSecret, accessToken, accessTokenSecret);
            
            byte[] fileContent = new byte[sourceBlob.Properties.Length];
            sourceBlob.DownloadToByteArray(fileContent, 0);

            var media = Upload.UploadImage(fileContent);

            var tweet = Tweet.PublishTweet(" ", new PublishTweetOptionalParameters
            {
                Medias = new List<IMedia> { media }
            });

            log.Info($"Tweeted with ID '{tweet.Id}'");

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
