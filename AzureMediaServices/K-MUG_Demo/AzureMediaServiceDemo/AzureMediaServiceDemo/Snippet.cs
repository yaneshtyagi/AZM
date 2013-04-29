using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureMediaServiceDemo.Snippet
{
    class Program
    {
        static string accountName = "kmugdemo";
        static string accountKey = "GhQ3/qOjofMI6ph7CUsftCIMjXJw2chIjR5Fg4Pk4OE=";

        static CloudMediaContext context = null;

        static void Main1(string[] args)
        {
            //Get context
            Console.WriteLine("Creating context...");
            context = GetContext();
            Console.WriteLine("Done");

            //create and upload asset
            IAsset asset = CreateAssetAndUploadSingleFile(AssetCreationOptions.None, @"C:\Demo\PenguinsSmall.jpg");


            //List and Download assets
            GetAllAssets();

            Console.Write("Press enter to exit...");
            Console.ReadLine();
        }

        static CloudMediaContext GetContext()
        {
            return new CloudMediaContext(accountName, accountKey);
        }

        static private IAsset CreateEmptyAsset(string assetName, AssetCreationOptions assetCreationOptions)
        {
            var asset = context.Assets.Create(assetName, assetCreationOptions);
            Console.WriteLine("Asset name: " + asset.Name);
            Console.WriteLine("Time cerated: " + asset.Created.Date.ToString());
            return asset;

        }

        static public IAsset CreateAssetAndUploadSingleFile(AssetCreationOptions assetCreationOptions, string singleFilePath)
        {

            //create enpty asset
            var assetName = Path.GetFileName(singleFilePath) + "_UTC " + DateTime.UtcNow.ToString();
            var asset = CreateEmptyAsset(assetName, assetCreationOptions);

            //Create asset file
            var fileName = Path.GetFileName(singleFilePath);
            var assetFile = asset.AssetFiles.Create(fileName);
            Console.WriteLine("Created assetFile {0}", assetFile.Name);

            //Create access policy
            var accessPolicy = context.AccessPolicies.Create(assetName, TimeSpan.FromDays(3), AccessPermissions.Write | AccessPermissions.List);

            //create locator
            var locator = context.Locators.CreateLocator(LocatorType.Sas, asset, accessPolicy);

            //Upload asset file
            Console.WriteLine("Upload {0}", assetFile.Name);
            assetFile.Upload(singleFilePath);
            Console.WriteLine("Done uploading of {0} using Upload()", assetFile.Name);

            //cleanup
            locator.Delete();
            accessPolicy.Delete();

            return asset;

        }

        private static List<IAsset> GetAllAssets()
        {
            var assetList = new List<IAsset>();
            foreach (IAsset asset in context.Assets)
            {
                Console.WriteLine(asset.Name);
                foreach (IAssetFile file in asset.AssetFiles)
                {
                    Console.WriteLine("\tdownloading: " + file.Name);
                    string localDownloadPath = Path.Combine(@"C:\Demo\Downloads", file.Name + DateTime.UtcNow);
                    file.Download(localDownloadPath);
                    Console.WriteLine("Downloaded: " + localDownloadPath);
                }
            }

            return assetList;
        }

    }
}
