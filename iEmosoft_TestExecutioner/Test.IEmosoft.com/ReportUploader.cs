using aUI.Automation.HelperObjects;
//using Limilabs.FTP.Client;
using System;
using System.Collections.Generic;
using System.IO;

namespace aUI.Automation.Test.IEmosoft.com
{
    public class ReportUploader : IReportUploader
    {
        IAutomationConfiguration Configuration;
        ReportFilesManager FileManager;
        private string SessionKey = null;
        //private Ftp FtpRequest;
        private string ApplicationId;
        private string TestName;
        private string LocationOfReport;
        private string CurrentRemoteDirectory = "";


        public ReportUploader(IAutomationConfiguration configuration)
        {
            Configuration = configuration;
            SessionKey = Guid.NewGuid().ToString();
            ApplicationId = ApplicationId.isNull() ? configuration.ApplicationUnderTest : ApplicationId;
        }

        public string UploadReport(string testName, List<string> filesToUpload, bool? deleteFilesAfterUpload = null)
        {
            if (Configuration.FTPUploadURL.isNull())
            {
                return string.Empty;
            }

            if (!deleteFilesAfterUpload.HasValue)
            {
                deleteFilesAfterUpload = Configuration.FTPUpload_DeleteLocalFilesAfterUploadComplete;
            }

            TestName = testName;
            if (Configuration.TestReportFilePath.IsNotNull())
            {
                FileManager = new ReportFilesManager(filesToUpload, Configuration.TestReportFilePath);
                return UploadLocalFilesFromALists(deleteFilesAfterUpload.Value);
            }

            return string.Empty;
        }

        public string UploadReport(string locationOfReport, string testName, bool? deleteLocalFilesAfterUpload, string applicationId = "")
        {
            string result = "";

            if (!deleteLocalFilesAfterUpload.HasValue)
            {
                deleteLocalFilesAfterUpload = Configuration.FTPUpload_DeleteLocalFilesAfterUploadComplete;
            }

            TestName = testName;
            LocationOfReport = locationOfReport;

            if (GetFileNamesToBeUplaoded())
            {
                result = UploadLocalFilesFromALists(deleteLocalFilesAfterUpload.Value);
            }

            return result;
        }

        private string UploadLocalFilesFromALists(bool delete)
        {
            string result = UploadReportFiles();
            UploadImageFiles();
            DisposeFTPConnection();

            if (delete)
            {
                DeleteLocalFilesIfApplicable();
            }
            else
            {
                FileManager.RestoreReportFileCotents();
            }

            return result;
        }

        private string UploadReportFiles()
        {
            try
            {
                string result = "";

                ConnectToServer();
                foreach (string rptFile in FileManager.ReportFiles)
                {
                    result = UploadFile(rptFile);
                }

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void UploadImageFiles()
        {
            ConnectToServer(true);
            foreach (string imgFile in FileManager.ImageFiles)
            {
                UploadFile(imgFile);
            }
        }

        private void DeleteLocalFilesIfApplicable()
        {
            try
            {
                Directory.Delete(LocationOfReport);
            }
            catch { }
        }

        private bool GetFileNamesToBeUplaoded()
        {
            var localReportFiles = new List<string>();
            if (Directory.Exists(LocationOfReport))
            {
                foreach (string fileName in Directory.GetFiles(LocationOfReport))
                {
                    if (fileName.EndsWith(".xlsx", StringComparison.InvariantCultureIgnoreCase) || fileName.EndsWith(".html", StringComparison.InvariantCultureIgnoreCase))
                    {
                        localReportFiles.Add(fileName);
                    }
                }

                foreach (string imageName in Directory.GetFiles(LocationOfReport + "\\ScreenCapture"))
                {
                    localReportFiles.Add(imageName);
                }
            }

            if (localReportFiles.Count > 0)
            {
                FileManager = new ReportFilesManager(localReportFiles, Configuration.TestReportFilePath);
            }
            return localReportFiles.Count > 0;
        }

        // TODO: This was marked obselete before, this FTP uploader needs refactoring
        private string ConnectToServer(bool navToScreenCapture = false)
        {
            CurrentRemoteDirectory = string.Format("{0}/{1}/{2}", ApplicationId, TestName, SessionKey);
            if (navToScreenCapture)
            {
                CurrentRemoteDirectory += "/ScreenCapture";
            }

            DisposeFTPConnection();
            //FtpRequest = new Ftp();
            //FtpRequest.Connect(Configuration.FTPUploadURL);
            //FtpRequest.Login(Configuration.FTPUploadUserName, Configuration.FTPUploadPassword);
            //FtpRequest.CreateFolder(CurrentRemoteDirectory);

            return CurrentRemoteDirectory;
        }

        // TODO: This was marked obselete before, this FTP uploader needs refactoring
        private void DisposeFTPConnection()
        {
            /*
            if (FtpRequest != null)
            {
                try
                {
                    FtpRequest.Close();
                }
                catch { }

                try
                {
                    FtpRequest.Dispose();
                }
                catch { }
            }
            */
        }

        // TODO: This was marked obselete before, this FTP uploader needs refactoring
        private string UploadFile(string file)
        {
            string remotePath = CurrentRemoteDirectory + "/" + Path.GetFileName(file);
            //FtpRequest.Upload(remotePath, file);

            return remotePath;
        }
    }

    public interface IReportUploader
    {
        string UploadReport(string testName, List<string> filesToUpload, bool? deleteFilesAfterUpload = null);
        string UploadReport(string locationOfReport, string testName, bool? deleteLocalFilesAfterUpload, string applicationId = "");
    }

    public class ReportFilesManager
    {
        List<string> imageFilesToUpload = new();
        List<string> reportFileNames = new();
        List<TestReportContents> reportFiles = new();
        public ReportFilesManager(List<string> files, string replacementText)
        {
            foreach (string file in files)
            {
                bool isReportFile = file.EndsWith("html") || file.Contains(".xls");

                if (isReportFile)
                {
                    reportFileNames.Add(file);
                    var c = new TestReportContents(file, replacementText);
                    reportFiles.Add(c);
                }
                else
                {
                    imageFilesToUpload.Add(file);
                }

            }
        }

        public List<string> ImageFiles
        {
            get
            {
                return imageFilesToUpload;
            }
        }

        public List<string> ReportFiles
        {
            get
            {
                return reportFileNames;
            }
        }

        public void RestoreReportFileCotents()
        {
            foreach (var r in reportFiles)
            {
                r.RestoreReportFileContents();
            }
        }
        private class TestReportContents
        {
            private bool fileContentWasChanged = false;

            public TestReportContents(string filePath, string replaceText)
            {
                if (filePath.EndsWith(".html"))
                {
                    IsHTMLFile = true;
                    Path = filePath;
                    OriginalContents = File.ReadAllText(filePath);

                    //This .html file is being uploaded to a server, it's current href="C:\\bla\bla\bla won't work
                    //need to strip the text off before uploading it, and restore the text when done uplaoding
                    if (!replaceText.isNull())
                    {
                        string newContents = OriginalContents.Replace(replaceText, "");
                        File.WriteAllText(filePath, newContents);
                        fileContentWasChanged = true;
                    }
                }
            }
            public string OriginalContents { get; set; }
            public string Path { get; set; }

            public bool IsHTMLFile { get; set; }

            public void RestoreReportFileContents()
            {
                if (fileContentWasChanged)
                {
                    File.WriteAllText(Path, OriginalContents);
                }
            }
        }
    }
}
