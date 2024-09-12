using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Waifuvault;
using System.Collections.Generic;
using System;

namespace Core.DirFiles
{
    /*
       
        Based on https://www.nuget.org/packages/Waifuvault documentaiton.
     
     */
    [SupportedOSPlatform("windows")]
    public class WaifuManage
    {
        public string URLorFile { get; set; }

        private string CurrentDirectory {  get; set; }
        private List<string> ExpireFormatList = ["d","m","h"];

        /// <summary>
        /// Waifuvault manager
        /// </summary>
        public WaifuManage() { }


        /// <summary>
        /// Create waifu bucket.
        /// </summary>
        public void CreateBucket()
        {
            var bucket = Task.Run(()=>Api.createBucket()).Result;
            FileSystem.SuccessWriteLine($"Your waifu bucket token: {bucket.token}");
        }

        /// <summary>
        /// Upload from file or URL.
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="onetimeDownload"></param>
        /// <param name="expires"></param>
        /// <param name="hidefileName"></param>
        /// <param name="password"></param>
        public void Upload(string bucket = "", bool onetimeDownload = false, string expires = "", bool hidefileName = false, string password = "")
        {
            var fileUrl = URLorFile;
            if (string.IsNullOrEmpty(fileUrl))
            {
                FileSystem.ErrorWriteLine("You need to specify the path to file or URL. Use -h for more information!");
                return;
            }

            var isParamPresent = ExpireFormatList.Any(param => expires.EndsWith(param));
            if (!isParamPresent) {
                FileSystem.ErrorWriteLine("You need to use the expire data format. Use -h for more information!");
                return;
            }

            if (!fileUrl.StartsWith("http"))
            {
                CurrentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);
                fileUrl = FileSystem.SanitizePath(fileUrl, CurrentDirectory);
                if (!File.Exists(fileUrl))
                {
                    FileSystem.ErrorWriteLine($"File does not exist: {fileUrl}. Use -h for more information!");
                    return;
                }
            }
            var uploadFile = new FileUpload(fileUrl, bucket, expires,password,hidefileName,onetimeDownload);
            var uploadResp = Task.Run(()=>Api.uploadFile(uploadFile)).Result;
            var tokenInfo = Task.Run(() => Api.fileInfo(uploadResp.token, true)).Result;
            FileSystem.SuccessWriteLine($"File         {fileUrl} was uploaded!");
            if(!string.IsNullOrEmpty(bucket))
                FileSystem.SuccessWriteLine($"Bucket       {bucket}");
            FileSystem.SuccessWriteLine($"URL:         {uploadResp.url}");
            FileSystem.SuccessWriteLine($"Token:       {uploadResp.token}");
            FileSystem.SuccessWriteLine($"Expire date: {tokenInfo.retentionPeriod}");
        }

        /// <summary>
        /// Delete file by token.
        /// </summary>
        /// <param name="token"></param>
        public void DeleteFile(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                FileSystem.ErrorWriteLine("You need to specify the uploaded file token. Use -h for more information!");
                return;
            }
            var deleted = Task.Run(()=>Api.deleteFile(token)).Result;
            if (deleted)
                FileSystem.SuccessWriteLine($"File with token {token} was deleted!");
            else
                FileSystem.SuccessWriteLine($"File with {token} was NOT deleted!");
        }

        /// <summary>
        /// Delete bucket token.
        /// </summary>
        /// <param name="bucketToken"></param>
        public void DeleteBucket(string bucketToken)
        {
            if (string.IsNullOrEmpty(bucketToken))
            {
                FileSystem.ErrorWriteLine("You need to specify the bucket token. Use -h for more information!");
                return;
            }
            var resp = Task.Run(()=> Api.deleteBucket(bucketToken)).Result;
            if(resp)
                FileSystem.SuccessWriteLine($"Bucket {bucketToken} was deleted!");
            else
                FileSystem.SuccessWriteLine($"Bucket {bucketToken} was NOT deleted!");
        }

        /// <summary>
        /// Get uploaded file info
        /// </summary>
        /// <param name="fileToken"></param>
        public void GetFileInfo(string fileToken)
        {
            if (string.IsNullOrEmpty(fileToken))
            {
                FileSystem.ErrorWriteLine("You need to specify the uploaded file token. Use -h for more information!");
                return;
            }
            var tokenInfo = Task.Run(()=>Api.fileInfo(fileToken, true)).Result;
            FileSystem.SuccessWriteLine($"URL:                {tokenInfo.url}");
            FileSystem.SuccessWriteLine($"Expire date:        {tokenInfo.retentionPeriod}");
            FileSystem.SuccessWriteLine($"Bucket:             {tokenInfo.bucket}");
            FileSystem.SuccessWriteLine($"Hidden Name:        {tokenInfo.options.hideFilename}");
            FileSystem.SuccessWriteLine($"Password protected: {tokenInfo.options.fileprotected}");
        }

        /// <summary>
        /// List file from bucket
        /// </summary>
        /// <param name="bucketToken"></param>
        public void ListBucketFiles(string bucketToken)
        {
            var bucket =Task.Run(()=>Api.getBucket(bucketToken)).Result;
            foreach (var file in bucket.files)
            {
                FileSystem.SuccessWriteLine($"-----------------------------------------------");
                FileSystem.SuccessWriteLine($"URL:                {file.url}");
                FileSystem.SuccessWriteLine($"File Token:         {file.token}");
                FileSystem.SuccessWriteLine($"Expire date:        {FileSystem.EpohConverter(long.Parse(file.retentionPeriod))}");
                FileSystem.SuccessWriteLine($"Hidden Name:        {file.options.hideFilename}");
                FileSystem.SuccessWriteLine($"Password protected: {file.options.fileprotected}");
            }
        }

        /// <summary>
        /// List vault restrictions.
        /// </summary>
        public void ListRestrictions()
        {
            Api.clearRestrictions();
            var restrictions =Task.Run(()=> Api.getRestrictions()).Result;
            foreach (var restriction in restrictions.Restrictions)
            {
                var restrictionType = restriction.type;
                if (restrictionType.Contains("MAX_")) 
                    FileSystem.SuccessWriteLine($"{restriction.type} : {FileSystem.GetSize(restriction.value,false)}");
                else
                    FileSystem.SuccessWriteLine($"{restriction.type} : {restriction.value}");
            }
        }
    }
}
