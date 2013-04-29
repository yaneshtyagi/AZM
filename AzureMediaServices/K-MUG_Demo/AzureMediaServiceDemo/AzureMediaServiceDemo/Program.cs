using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureMediaServiceDemo
{
    class Program
    {
        static string accountName = "kmugdemo";
        static string accountKey = "";

        static CloudMediaContext context = null;

        static void Main(string[] args)
        {
            GetContext();
            UploadAssetFile(AssetCreationOptions.None, @"C:\Demo\PenguinsSmall.jpg");
            
            Console.Write("Press enter to exit...");
            Console.ReadLine();
        }

        static void GetContext()
        {
            Console.WriteLine("Aquiring context...");
            context = new CloudMediaContext(accountName, accountKey);
            Console.WriteLine("Done");
        }

        static private IAsset CreateEmptyAsset(string assetName, AssetCreationOptions assetCreationOptions)
        {
            var asset = context.Assets.Create(assetName, assetCreationOptions);
            Console.WriteLine("Asset name: " + asset.Name);
            Console.WriteLine("Time cerated: " + asset.Created.Date.ToString());
            return asset;

        }

        static public IAsset UploadAssetFile(AssetCreationOptions assetCreationOptions, string singleFilePath)
        {
            //Create Asset
            var assetName = Path.GetFileName(singleFilePath) + "_UTC " + DateTime.UtcNow.ToString();
            var asset = CreateEmptyAsset(assetName, assetCreationOptions);
            
            //Create Asset File
            var fileName = Path.GetFileName(singleFilePath);
            var assetFile = asset.AssetFiles.Create(fileName);
            Console.WriteLine("\tCreated assetFile {0}", assetFile.Name);
            
            //Create Access Policy
            var accessPolicy = context.AccessPolicies.Create(assetName, TimeSpan.FromDays(3), AccessPermissions.Write | AccessPermissions.List);
            
            //Create Locator
            var locator = context.Locators.CreateLocator(LocatorType.Sas, asset, accessPolicy);
            
            //Upload AssetFile
            Console.WriteLine("\tUploading {0}...", assetFile.Name);
            assetFile.Upload(singleFilePath);
            Console.WriteLine("\t\tDone");

            //CLeanup
            locator.Delete();
            accessPolicy.Delete();

            return asset;
        }

    }
}


