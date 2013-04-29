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
        static string accountName = "kmugdemo";
        static string accountKey = ""; //get from portal
        static string accountLocation = "";
        static CloudMediaContext context = null;
        static string outputFilesFolder = "Log.txt";

        static void Main(string[] args)
        {
            context = GetContext();
            //CreateAssetAndUploadSingleFile(AssetCreationOptions.None, @"C:\Users\Yanesh\Pictures\LinkedinContentFilters.png");

            List<IAsset> assetList = GetAllAssets(ref context);
            //DownloadAsset(jobID, @"D:\Temp\Azure");

            Console.ReadLine();
        }

        private static List<IAsset> GetAllAssets(ref CloudMediaContext context)
        {
            var assetList = new List<IAsset>();
            foreach (IAsset asset in context.Assets)
            {
                Console.WriteLine(asset.Name);
                foreach (IAssetFile file in asset.AssetFiles)
                {
                    Console.WriteLine("\tdownloading: " + file.Name);
                    string localDownloadPath = Path.Combine(@"D:\Temp\Azure", file.Name);
                    file.Download(localDownloadPath);
                    Console.WriteLine("Downloaded: " + localDownloadPath);
                }
            }

            return assetList;
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
            var assetName = Path.GetFileName(singleFilePath) + "_UTC " + DateTime.UtcNow.ToString();
            var asset = CreateEmptyAsset(assetName, assetCreationOptions);
            var fileName = Path.GetFileName(singleFilePath);
            var assetFile = asset.AssetFiles.Create(fileName);
            Console.WriteLine("Created assetFile {0}", assetFile.Name);
            var accessPolicy = context.AccessPolicies.Create(assetName, TimeSpan.FromDays(3), AccessPermissions.Write | AccessPermissions.List);
            var locator = context.Locators.CreateLocator(LocatorType.Sas, asset, accessPolicy);
            Console.WriteLine("Upload {0}", assetFile.Name);
            assetFile.Upload(singleFilePath);
            Console.WriteLine("Done uploading of {0} using Upload()", assetFile.Name);

            locator.Delete();
            accessPolicy.Delete();

            return asset;

        }

        //Get a Media Processor instance
        private static IMediaProcessor GetLatestMediaProcessorByName(string mediaProcessorName)
        {
            var processor = context.MediaProcessors.Where(p => p.Name == mediaProcessorName).
               ToList().OrderBy(p => new Version(p.Version)).LastOrDefault();

            if (processor == null)
                throw new ArgumentException(string.Format("Unknown media processor", mediaProcessorName));

            return processor;
        }

        //Check Job Progress
        private static void StateChanged(object sender, JobStateChangedEventArgs e)
        {
            Console.WriteLine("Job state changed event:");
            Console.WriteLine("  Previous state: " + e.PreviousState);
            Console.WriteLine("  Current state: " + e.CurrentState);

            switch (e.CurrentState)
            {
                case JobState.Finished:
                    Console.WriteLine();
                    Console.WriteLine("********************");
                    Console.WriteLine("Job is finished.");
                    Console.WriteLine("Please wait while local tasks or downloads complete...");
                    Console.WriteLine("********************");
                    Console.WriteLine();
                    Console.WriteLine();
                    break;
                case JobState.Canceling:
                case JobState.Queued:
                case JobState.Scheduled:
                case JobState.Processing:
                    Console.WriteLine("Please wait...\n");
                    break;
                case JobState.Canceled:
                case JobState.Error:
                    // Cast sender as a job.
                    IJob job = (IJob)sender;
                    // Display or log error details as needed.
                    LogJobStop(job.Id);
                    break;
                default:
                    break;
            }
        }

        private static void LogJobStop(string jobID)
        {
            StringBuilder builder = new StringBuilder();
            IJob job = GetJob(jobID);

            builder.AppendLine("\nThe job stopped due to cancellation or an error.");
            builder.AppendLine("***************************");
            builder.AppendLine("Job ID: " + job.Id);
            builder.AppendLine("Job Name: " + job.Name);
            builder.AppendLine("Job State: " + job.State.ToString());
            builder.AppendLine("Job started (server UTC time): " + job.StartTime.ToString());
            builder.AppendLine("Media Services account name: " + accountName);
            builder.AppendLine("Media Services account location: " + accountLocation);
            // Log job errors if they exist.  
            if (job.State == JobState.Error)
            {
                builder.Append("Error Details: \n");
                foreach (ITask task in job.Tasks)
                {
                    foreach (ErrorDetail detail in task.ErrorDetails)
                    {
                        builder.AppendLine("  Task Id: " + task.Id);
                        builder.AppendLine("    Error Code: " + detail.Code);
                        builder.AppendLine("    Error Message: " + detail.Message + "\n");
                    }
                }
            }
            builder.AppendLine("***************************\n");
            // Write the output to a local file and to the console. The template 
            // for an error output file is:  JobStop-{JobId}.txt
            string outputFile = outputFilesFolder + @"\JobStop-" + JobIdAsFileName(job.Id) + ".txt";
            File.AppendAllText(outputFile, builder.ToString());
            Console.Write(builder.ToString());


        }

        private static string JobIdAsFileName(string jobID)
        {
            return jobID.Replace(":", "_");
        }

        static IJob GetJob(string jobId)
        {
            // Use a Linq select query to get an updated 
            // reference by Id. 
            var jobInstance =
                from j in context.Jobs
                where j.Id == jobId
                select j;
            // Return the job reference as an Ijob. 
            IJob job = jobInstance.FirstOrDefault();

            return job;
        }

        /// <summary>
        /// Too complicated. The other method is pretty much simple.
        /// </summary>
        /// <param name="jobID"></param>
        /// <param name="outputFolder"></param>
        /// <returns></returns>
        static IAsset DownloadAsset(string jobID, string outputFolder)
        {
            IJob job = GetJob(jobID);
            IAsset outputAsset = job.OutputMediaAssets[0];
            IAccessPolicy accessPolicy = context.AccessPolicies.Create("File Download Policy", TimeSpan.FromDays(30), AccessPermissions.Read);
            ILocator locator = context.Locators.CreateLocator(LocatorType.Sas, outputAsset, accessPolicy);

            BlobTransferClient client = new BlobTransferClient()
            {
                NumberOfConcurrentTransfers = 10,
                ParallelTransferThreadCount = 10
            };

            var downloadTasks = new List<Task>();
            foreach (IAssetFile outputFile in outputAsset.AssetFiles)
            {
                string localDownloadPath = Path.Combine(outputFolder, outputFile.Name);
                Console.Write("File download path: " + localDownloadPath);
                downloadTasks.Add(
                    outputFile.DownloadAsync(
                        Path.GetFullPath(localDownloadPath),
                        client,
                        locator,
                        CancellationToken.None)
                    );
            }
            Task.WaitAll(downloadTasks.ToArray());
            return outputAsset;

        }



    }


}
