using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Remote;

namespace aUI.Automation.Flutter
{
    public class FlutterDriver : AppiumDriver<FlutterElement>
    {
        // TODO: May need more constructors
        public FlutterDriver(Uri remoteAddress, DriverOptions appiumOptions) :
            base(remoteAddress, DriverOptionsToCapabilities(appiumOptions))
        {
        }


        protected override RemoteWebElementFactory CreateElementFactory()
        {
            return new AndroidElementFactory(this);
        }

        internal static ICapabilities DriverOptionsToCapabilities(DriverOptions options)
        {
            // TODO: This will need to support android and iOS
            options.AddAdditionalCapability("appium:platformName", "Android");
            return options.ToCapabilities();
        }

        /// <summary>
        /// Finds a Flutter Element. Waits a default of 0.5 for Flutter to find the element.
        /// </summary>
        /// <param name="by"></param>
        /// <returns>A flutter element IF the element exists. Otherwise null</returns>
        public FlutterElement FindElement(FlutterBy by, int milliseconds = 500)
        {
            // WaitFor 500ms
            var response = WaitFor(by, milliseconds);
            if (response == null)
            {
                // This should never happen as all paths through ExecuteFlutterScript return a new object
                throw new Exception("FlutterDriver: response was null");
            }
            else
            {
                if (response is FlutterResponse)
                {
                    return new FlutterElement(this, by);
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Performs a Flutter waitFor.
        /// </summary>
        /// <param name="by">Finder</param>
        /// <param name="milliseconds">Time to wait for element to exist</param>
        /// <returns>FlutterResponse if found, FlutterError if not</returns>
        public IFlutterResult WaitFor(FlutterBy by, int milliseconds)
        {
            return ExecuteFlutterScript("flutter:waitFor", by.Encoded(), milliseconds);
        }

        /// <summary>
        /// Executes a script. Meant mainly for flutter commands to be passed through.
        /// </summary>
        /// <param name="script"></param>
        /// <param name="args">Arguments to be passed into script</param>
        /// <returns>Either FlutterResponse or FlutterError</returns>
        public IFlutterResult ExecuteFlutterScript(string script, params object[] args)
        {
            try
            {
                var rsp = ExecuteScript(script, args);
                return new FlutterResponse()
                {
                    Response = rsp.ToString(),
                };
            }
            catch (WebDriverException e)
            {
                var message = e.Message;
                if (!message.StartsWith("An unknown server-side error occurred while processing the command."))
                {
                    // This is not the expected error from appium-flutter-driver. Rethrow
                    throw;
                }
                return FlutterError.Parse(message);
            }
        }

        /// <summary>
        /// Scroll a flutter element. 
        /// </summary>
        /// <param name="by">finder for element to scroll</param>
        /// <param name="dx">negative for scrolling left, positive for scrolling right</param>
        /// <param name="dy">negative for scrcolling down, positive for scrolling up</param>
        /// <param name="milliseconds">amount of milliseconds the gesture should take. 50 seems to be the fastest possible.</param>
        /// <returns>FlutterResponse if ok, else FlutterError</returns>
        /// <exception cref="Exception"></exception>
        public IFlutterResult Scroll(FlutterBy by, double dx, double dy, int milliseconds)
        {
            var result = ExecuteFlutterScript("flutter:scroll", by.Encoded(), new Dictionary<string, object> {
                { "dx", dx },
                { "dy", dy },
                { "durationMilliseconds", milliseconds },
                { "frequency", 60 }
            });
            if (result == null)
            {
                throw new Exception("Scroll returned null");
            }
            return result;
        }

        /// <summary>
        /// Assumes by is already on screen, then tries to center it according to alignment. 
        /// </summary>
        /// <param name="by">finder for element</param>
        /// <param name="alignment"></param>
        /// <param name="milliseconds">timeout in milliseconds</param>
        /// <returns>true if successful, else false</returns>
        /// <exception cref="Exception"></exception>
        public IFlutterResult ScrollIntoView(FlutterBy by, double alignment = 0.0, int milliseconds = 1000)
        {
            var result = ExecuteFlutterScript("flutter:scrollIntoView", by.Encoded(), new Dictionary<string, object>
            {
                { "alignment", alignment },
                { "timeout", milliseconds }
            });
            if (result == null)
            {
                throw new Exception("ScrollIntoView returned null");
            }
            // Basic check, if not error assume it was successful.
            return result;
        }

        /// <summary>
        /// Scroll list until element is loaded.
        /// </summary>
        /// <param name="list">The scrollable widget which contains element. In most cases list will be FlutterBy.Type("ListView") or FlutterBy.Type("GridView"). </param>
        /// <param name="element">The element we are trying to find</param>
        /// <returns>true if found, false if not found</returns>
        public bool ScrollUntilLoaded(FlutterBy list, FlutterBy element)
        {

            // Always scroll to top of list and work down
            // Scroll 100,000px up, which will definitely put us at the top of the list, no matter how far down we are.
            Scroll(list, 0, 100000, 50);
            var startTime = DateTime.Now;
            var elementOnScreen = false;
            // Repeat for 2 seconds, scroll down a little and check if element is visible.
            while (DateTime.Now - startTime < TimeSpan.FromMilliseconds(2000) && !elementOnScreen)
            {
                var currentResponse = WaitFor(element, 10);    // 10ms wait for element to be on screen
                if (currentResponse is FlutterError)
                {
                    // Element not on screen, scroll down
                    Scroll(list, 0, -500, 50); // Scroll down 500px in 50ms
                }
                else
                {
                    // Element was found
                    elementOnScreen = true;
                }
            }
            return elementOnScreen;
            // Potential performance improvement: Appium-flutter-driver has a method which does this all server-side,
            // which would cut down on time for network calls.
        }

        /// <summary>
        /// Scrolls an element into view. Tries to place it close to center of screen
        /// </summary>
        /// <param name="scrollableParent">parent element which is scrollable</param>
        /// <param name="element">element we are looking for</param>
        /// <returns></returns>
        public bool ScrollTo(FlutterBy scrollableParent, FlutterBy element)
        {
            var elementAlreadyOnScreen = WaitFor(element, 10);
            if (elementAlreadyOnScreen is FlutterError)
            {
                var loaded = ScrollUntilLoaded(scrollableParent, element); // Scroll until element is loaded
                if (!loaded)
                {
                    return false;   // Failed to load element
                }
            }
            var inView = ScrollIntoView(element); // Scroll element until its centered
            if (inView is FlutterError)
            {
                return false;   // Failed to align element
            }
            return true;
        }
    }

    /// <summary>
    /// Represents either a valid flutter response or error from flutter
    /// </summary>
    public interface IFlutterResult
    {
    }
    public class FlutterResponse : IFlutterResult
    {
        public string Response
        {
            get; init;
        }
    }
    /// <summary>
    /// Represents an error returned by Flutter.
    /// This is a record so "==" will compare two FlutterError's by value. For example flutterError == FlutterError.Nothing ? empty error : not an empty error
    /// </summary>
    public record FlutterError : IFlutterResult
    {
        public static readonly FlutterError Nothing = new() { IsError = false, Method = "", Response = "", Type = "" };
        public static FlutterError Parse(string flutterError)
        {
            var matcher = new Regex(@".*Original error: (.*), server response {\s+""isError"": (false|true),\s+""response"": ""(.*)"",\s+""type"": ""(.*)"",\s+""method"": ""(.*)""\s+}");
            var groups = matcher.Match(flutterError).Groups;
            if (groups.Count != 6)
            {
                throw new Exception($"Unrecognized error: {flutterError}");
            }
            return new FlutterError()
            {
                OriginalError = groups[1].Value,
                IsError = bool.Parse(groups[2].Value),
                Response = groups[3].Value,
                Type = groups[4].Value,
                Method = groups[5].Value
            };
        }
        public string OriginalError
        {
            get; init;
        }
        public bool IsError
        {
            get; init;
        }
        public string Response
        {
            get; init;
        }
        public string Type
        {
            get; init;
        }
        public string Method
        {
            get; init;
        }
    }
}

