using System;
using System.Linq;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.MediaServices.Client;

namespace AZM_FirstTry
{
    class Program
    {
        static string accountName = "";
        static string accountKey = "";
        static CloudMediaContext context = null;

        static void Main(string[] args)
        {
            context = GetContext();
        }

        static CloudMediaContext GetContext()
        {
            return new CloudMediaContext(accountName, accountKey);
        }

        static private IAsset CreateEmptyAsset(string assetName, AssetCreationOptions assetCreationOptions)
        {
            var asset = context.Assets.Create(assetName, assetCreationOptions);
            Console.WriteLine("Asset name: " + asset.Name);
            Console.WriteLine("Time cerated: " + asset.Created.Date.ToString();
            return asset;

        }
    }


}
