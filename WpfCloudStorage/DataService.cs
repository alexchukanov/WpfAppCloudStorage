using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WpfCloudStorage
{
	public class DataService
	{
		public static async Task<ushort[]> SendRequest(string fullUrl)
		{
			ushort[] target = new ushort[0];
			using (var client = new HttpClient())
			{
				HttpResponseMessage result = await client.GetAsync(fullUrl);
				if (result.IsSuccessStatusCode)
				{
					var array8 = await result.Content.ReadAsByteArrayAsync();

					target = new ushort[array8.Length / 2];

					Buffer.BlockCopy(array8, 0, target, 0, array8.Length);

					return target;
				}
			}

			return target;
		}

		private static string ConnectionSting
		{
			get
			{
				return "DefaultEndpointsProtocol=https;AccountName=pacificcelerity;AccountKey={0};EndpointSuffix=core.windows.net";
			}
		}

		//load with key
		public static async Task<ushort[]> Load16(string blobName, string accessKey)
		{
			var containerName = "uploadimage";

			string connStr = string.Format(ConnectionSting, accessKey);

			// create object of your storage account  
			CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connStr);

			// create the client of your storage account  
			CloudBlobClient client = storageAccount.CreateCloudBlobClient();

			// create reference of container  
			CloudBlobContainer container = client.GetContainerReference(containerName);

			// get a particular blob within that container  
			CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

			// get list of all blobs in the container  
			//var allBlobs = container.ListBlobs();

			// convert the blob to memorystream  
			MemoryStream memStream = new MemoryStream();
			await blockBlob.DownloadToStreamAsync(memStream);

			byte[] array8 = memStream.ToArray();

			ushort[] target = new ushort[array8.Length / 2];

			Buffer.BlockCopy(array8, 0, target, 0, array8.Length);

			return target;
		}


		//upload
		public static async Task<bool> UpLoad16(string blobName, string accessKey, string containerName, byte[] pix16)
		{
			bool res = false;

			try
			{
				string connStr = string.Format(ConnectionSting, accessKey);

				// create object of storage account  
				CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connStr);


				// create client of storage account  
				CloudBlobClient client = storageAccount.CreateCloudBlobClient();

				// create the reference of your storage account  
				CloudBlobContainer container = client.GetContainerReference(containerName);

				// check if the container exists or not in your account  
				//var isCreated = container.CreateIfNotExists();

				// set the permission to blob type  
				//await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

				CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

				await blob.UploadFromByteArrayAsync(pix16, 0, pix16.Count());

				res = true;
			}
			catch (Exception ex)
			{
				throw;
			}

			return res;
		}
	}
}
