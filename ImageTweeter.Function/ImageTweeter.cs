using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Linq;
using System.Threading;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using Tweetinvi.Core.Public.Parameters;
using Tweetinvi.Core.Public.Models.Enum;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ImageTweeter
{
    public static class ImageTweeter
    {
        [FunctionName("ImageTweeter")]
        public static async Task Run([TimerTrigger("%TimerSchedule%", RunOnStartup = true)]TimerInfo myTimer, ILogger log, 
        Microsoft.Azure.WebJobs.ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
            .SetBasePath(context.FunctionAppDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(config["StorageConnectionString"]);

            //todo these should prolly be in config
            const string UnprocessedContainerName = "unprocessedimages";
            const string ProcessedContainerName = "processedimages";

            //grab file

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer unprocessedContainer = blobClient.GetContainerReference(UnprocessedContainerName);
            
            var unprocessedBlobs = await GetCloudBlockBlobs(unprocessedContainer);
            var sourceBlob = unprocessedBlobs.OrderBy(b => Guid.NewGuid()).First(); //pick random blob. yes this is a silly and slow way to do it

            log.LogInformation($"About to Tweet blob '{sourceBlob.Name}'");

            //tweet

            var consumerKey = config["TwitterConsumerKey"];
            var consumerSecret = config["TwitterConsumerSecret"];
            var accessToken = config["TwitterAccessToken"];
            var accessTokenSecret = config["TwitterAccessTokenSecret"];

            ExceptionHandler.SwallowWebExceptions = false;
            Auth.SetUserCredentials(consumerKey, consumerSecret, accessToken, accessTokenSecret);

            byte[] fileContent = new byte[sourceBlob.Properties.Length];
            await sourceBlob.DownloadToByteArrayAsync(fileContent, 0);

            log.LogInformation($"Downloaded file. Byte count: {fileContent.Length}");

            var media = Upload.UploadImage(fileContent);

            log.LogInformation("Uploaded file.");

            log.LogInformation($"Uploaded file: {media.HasBeenUploaded}. MediaId: {media.MediaId}.");

            var tweet = Tweet.PublishTweet(" ", new PublishTweetOptionalParameters
            {
                Medias = { media }
            });

            if(tweet == null)
            {
                throw new Exception("Tweet object returned by TweetInvi was null");
            }

            log.LogInformation($"Tweeted with ID '{tweet.Id}'");

            //move file
            CloudBlobContainer processedContainer = blobClient.GetContainerReference(ProcessedContainerName);
            var destinationBlob = processedContainer.GetBlockBlobReference(sourceBlob.Name);
            await destinationBlob.StartCopyAsync(sourceBlob);
            //seems sketchy to not confirm that the copy has finished before deleting, but some StackOverflow rando said it's OK!
            await sourceBlob.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots,null, null, null);
            log.LogInformation($"Moved file to '{ProcessedContainerName}'");
        }

        private async static Task<List<CloudBlockBlob>> GetCloudBlockBlobs(CloudBlobContainer container)
        {
            var ret = new List<IListBlobItem>();
            BlobContinuationToken continuationToken = null;

            do
            {
                var segment = await container.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = segment.ContinuationToken;
                ret.AddRange(segment.Results);
            } while (continuationToken != null);

            //todo: handle non-CloudBlobs gracefully
            return ret.Cast<CloudBlockBlob>().ToList();
        }
    }
}
