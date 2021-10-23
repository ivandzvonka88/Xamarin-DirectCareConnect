using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.ModelsLegacy
{
    public class AzureHelper
    {
        CloudBlobContainer _credentialBlobContainer;
        public AzureHelper(string containerCode, string blobConnection)
        {
            var storageAccount = CloudStorageAccount.Parse(blobConnection);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            _credentialBlobContainer = blobClient.GetContainerReference(containerCode.ToLower());
            _credentialBlobContainer.CreateIfNotExists();
        }

        public void AddToBlob(byte[] data, string fileName)
        {
            var blob = _credentialBlobContainer.GetBlockBlobReference(fileName);
            blob.DeleteIfExists();
            blob.UploadFromByteArray(data, 0, data.Length);
        }
    }
}