using System;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace ImageTweeter.Authenticator
{
    // This is a helper app for getting 
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
            .AddJsonFile("local.settings.json", optional: false)
            .Build();

            // Create a new set of credentials for the application.
            var appCredentials = new TwitterCredentials(config["TwitterAccountConsumerKey"], config["TwitterAccountConsumerSecret"]);

            Console.WriteLine($"{config["TwitterAccountConsumerKey"]}, {config["TwitterAccountConsumerSecret"]}");
            // Init the authentication process and store the related `AuthenticationContext`.
            var authenticationContext = AuthFlow.InitAuthentication(appCredentials);
            Console.WriteLine($"Go to {authenticationContext.AuthorizationURL}");

            Console.WriteLine("Enter the pin code given by Twitter");            
            var pinCode = Console.ReadLine();

            // With this pin code it is now possible to get the credentials back from Twitter
            var userCredentials = AuthFlow.CreateCredentialsFromVerifierCode(pinCode, authenticationContext);

            Console.WriteLine($"userCredentials:");
            Console.WriteLine($"Consumer Key: {userCredentials.ConsumerKey}");
            Console.WriteLine($"Consumer Secret: {userCredentials.ConsumerSecret}");
            Console.WriteLine($"Access Token: {userCredentials.AccessToken}");
            Console.WriteLine($"Access Token Secret: {userCredentials.AccessTokenSecret}");

            // Use the user credentials in your application
            // Auth.SetCredentials(userCredentials);
        }
    }
}
