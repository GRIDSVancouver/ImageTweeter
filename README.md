Super simple Azure function to tweet images on a schedule.

Images are stored in [Azure Blob Storage](https://azure.microsoft.com/en-us/services/storage/blobs/) and tweeted using the [TweetInvi](https://github.com/linvi/tweetinvi) library.

Really simple logic:

1. Grab an image from a Blob Storage container named `unprocessedimages`
1. Tweet that image (with no text)
1. Move that image to a container named `processedimages`

The schedule is currently defined by an Azure Function [timer trigger](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer) (basically just a CRON expression). Could easily be tweaked to use a different trigger.