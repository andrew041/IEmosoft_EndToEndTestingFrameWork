using aUI.Automation.HelperObjects;
using aUI.Automation.Interfaces;
using OpenQA.Selenium;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace aUI.Automation.ScreenCaptures
{
    class HeadlessScreenCapture : IScreenCapture
    {
        ScreenPhotographer Photo = null;
        private Screenshot Sc = null;
        private IUIDriver Driver = null;
        private int ImageNum = 0;
        private Image LastImg;

        public HeadlessScreenCapture(string rootPath, IUIDriver driver)
        {
            Driver = driver;
            Photo = new ScreenPhotographer(rootPath);
        }

        public void CaptureDesktop(string fileName, string textToOverlay, bool deleteDup = true)
        {
            Sc = null;
            try
            {
                Sc = ((ITakesScreenshot)Driver.RawWebDriver).GetScreenshot();
                //File.WriteAllBytes(fileName, Sc.AsByteArray);
                //Sc.SaveAsFile(fileName, ScreenshotImageFormat.Png);
                Bitmap bmp;
                using (var ms = new MemoryStream(Sc.AsByteArray))
                {
                    bmp = new Bitmap(ms);
                }

                if (!string.IsNullOrEmpty(textToOverlay))
                {
                    AddOverlay(textToOverlay, bmp);
                }
                //rtn.Save(fileName);
                bmp.Save(fileName);
                LastImg = (Image)bmp;

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch(Exception e)
            {
                var a = "";
                //ignore it, do nothing
            }
        }

        private Image CompressImage(int newWidth, int newHeight, int newQuality)   // set quality to 1-100, eg 50
        {
            if (LastImg != null)
            {
                using (Image memImage = new Bitmap(LastImg, newWidth, newHeight))
                {
                    ImageCodecInfo myImageCodecInfo;
                    System.Drawing.Imaging.Encoder myEncoder;
                    EncoderParameter myEncoderParameter;
                    EncoderParameters myEncoderParameters;
                    myImageCodecInfo = GetEncoderInfo("image/jpeg");
                    myEncoder = System.Drawing.Imaging.Encoder.Quality;
                    myEncoderParameters = new EncoderParameters(1);
                    myEncoderParameter = new EncoderParameter(myEncoder, newQuality);
                    myEncoderParameters.Param[0] = myEncoderParameter;

                    MemoryStream memStream = new MemoryStream();
                    memImage.Save(memStream, myImageCodecInfo, myEncoderParameters);
                    Image newImage = Image.FromStream(memStream);
                    ImageAttributes imageAttributes = new ImageAttributes();
                    using (Graphics g = Graphics.FromImage(newImage))
                    {
                        g.InterpolationMode =
                          System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;  //**
                        g.DrawImage(newImage, new Rectangle(Point.Empty, newImage.Size), 0, 0,
                          newImage.Width, newImage.Height, GraphicsUnit.Pixel, imageAttributes);
                    }
                    return newImage;
                }
            }
            return new Bitmap(1, 1, PixelFormat.DontCare);
        }

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo ici in encoders)
                if (ici.MimeType == mimeType) return ici;

            return null;
        }

        private void AddOverlay(string text, Bitmap bitmap)
        {
            using Graphics graphics = Graphics.FromImage(bitmap);
            using Font arialFont = new("Arial", 16);
            try
            {
                graphics.DrawString(Driver.CurrentFormName_OrPageURL, arialFont, Brushes.Red, new PointF(25, 80));
            }
            catch { }
            
            graphics.DrawString(text, arialFont, Brushes.Red, new PointF(25, 108));
        }

        public byte[] LastImageCapturedAsByteArray
        {
            get {
                if(LastImg == null)
                {
                    return Array.Empty<byte>();
                }
                if (Config.GetConfigSetting("ReportSmallImage", "true").ToLower().Equals("true"))
                {
                    var size = Driver.RawWebDriver.Manage().Window.Size;
                    var h = (int)Math.Floor(size.Height / 3.0);
                    var w = (int)Math.Floor(size.Width / 3.0);
                    var rtn = CompressImage(w, h, 100);
                    byte[] bts;

                    using (var stream = new MemoryStream())
                    {
                        rtn.Save(stream, ImageFormat.Png);
                        bts = stream.ToArray();
                    }
                    return bts;
                }

                return Sc.AsByteArray;
            }
        }

        public void Dispose()
        {}

        public string NewFileName
        {
            get
            {
                return Path.Combine(Photo.RootPath, string.Format("TestImage_{0}_{1}.png", ImageNum++, DateTime.Today.ToString("MM-dd-yyyy")));
            }
        }
    }
}
