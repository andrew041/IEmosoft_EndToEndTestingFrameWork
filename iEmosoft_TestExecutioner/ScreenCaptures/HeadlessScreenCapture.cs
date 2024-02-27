using aUI.Automation.HelperObjects;
using aUI.Automation.Interfaces;
using nQuant;
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
        private Bitmap LastImg;
        private int Height = 0;
        private int Width = 0;

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
                var size = Driver.RawWebDriver.Manage().Window.Size;
                Height = size.Height;
                Width = size.Width;
                Sc = ((ITakesScreenshot)Driver.RawWebDriver).GetScreenshot();

                Bitmap bmp;
                using (var ms = new MemoryStream(Sc.AsByteArray))
                {
                    bmp = new Bitmap(ms);
                }

                if (!string.IsNullOrEmpty(textToOverlay))
                {
                    AddOverlay(textToOverlay, bmp);
                }

                bmp.Save(fileName);
                LastImg = bmp;

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch(Exception e)
            {
                Console.WriteLine("ScrnShot: "+e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        private void GetImg(string text)
        {
            Sc = null;
            try
            {
                var size = Driver.RawWebDriver.Manage().Window.Size;
                Height = size.Height;
                Width = size.Width;
                Sc = ((ITakesScreenshot)Driver.RawWebDriver).GetScreenshot();
                Bitmap bmp;

                using (var ms = new MemoryStream(Sc.AsByteArray))
                {
                    bmp = new Bitmap(ms);
                }

                LastImg = bmp;

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception e)
            {
                Console.WriteLine("ScrnShot2: " + e.Message);
                Console.WriteLine(e.StackTrace);
            }
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
                    GetImg("");
                }
                if (Config.GetConfigSetting("ReportSmallImage", "true").ToLower().Equals("true"))
                {
                    try
                    {
                        WuQuantizer wuq = new WuQuantizer();
                        Image img = wuq.QuantizeImage(new Bitmap(LastImg, (int)Math.Floor(Width / 2.0), (int)Math.Floor(Height / 2.0)));

                        byte[] bts = null;
                        if (img != null)
                        {
                            using var stream = new MemoryStream();
                            img.Save(stream, ImageFormat.Png);
                            bts = stream.ToArray();
                        }
                        return bts;
                    } catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
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
