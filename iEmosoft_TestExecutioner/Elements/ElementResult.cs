using OpenQA.Selenium;
using System;

namespace aUI.Automation.Elements
{
    public class ElementResult
    {
        public TestExecutioner TE = null;


        public string Text = "";
        public string AttributeText = "";
        public string ElementName = "";
        public IWebElement RawEle = null;
        public bool Success = false;
        public Exception Exception = null;

        public ElementResult(TestExecutioner tE)
        {
            TE = tE;
        }

        /// <summary>
        /// Useful when a ElementResult should ALWAYS be successful.
        /// </summary>
        /// <remarks>
        /// Can be used like:
        /// <code>
        /// var labelText = eleResult.AssertSucess("reason").Text;
        /// </code>
        /// to both assert its always true and get the element's text
        /// </remarks>
        /// <param name="message">assertion message</param>
        /// <returns>this ElementResult</returns>
        public ElementResult AssertSuccess(string message)
        {
            TE.Assert.IsTrue(Success, message);
            return this;
        }

        /// <summary>
        /// Useful when a ElementResult should always fail.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public ElementResult AssertFailure(string message)
        {
            TE.Assert.IsTrue(!Success, message);
            return this;
        }
    }
}
