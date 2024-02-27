using NUnit.Framework;
using NUnit.Framework.Constraints;
namespace aUI.Automation.HelperObjects
{
    public class AssertHelp
    {
        public TestExecutioner TE;

        public AssertHelp(TestExecutioner tE)
        {
            TE = tE;
        }

        public void AreEqual(object expected, object actual, string step)
        {
            TE.BeginTestCaseStep($"Assert: {step}");
            try
            {
                Assert.AreEqual(expected, actual, step);
            }
            catch (AssertionException)
            {
                TE.FailCurrentStep(expected.ToString(), actual.ToString());
                throw;
            }
        }
        public void AreNotEqual(object expected, object actual, string step)
        {
            TE.BeginTestCaseStep($"Assert: {step}");
            try
            {
                Assert.AreNotEqual(expected, actual, step);
            }
            catch (AssertionException)
            {
                TE.FailCurrentStep(expected.ToString(), actual.ToString());
                throw;
            }
        }
        public void AreNotSame(object expected, object actual, string step)
        {
            TE.BeginTestCaseStep($"Assert: {step}");
            try
            {
                Assert.AreNotSame(expected, actual, step);
            }
            catch (AssertionException)
            {
                TE.FailCurrentStep(expected.ToString(), actual.ToString());
                throw;
            }
        }
        public void AreSame(object expected, object actual, string step)
        {
            TE.BeginTestCaseStep($"Assert: {step}");
            try
            {
                Assert.AreSame(expected, actual, step);
            }
            catch (AssertionException)
            {
                TE.FailCurrentStep(expected.ToString(), actual.ToString());
                throw;
            }
        }

        public void Contains(object expected, System.Collections.ICollection actual, string step)
        {
            TE.BeginTestCaseStep($"Assert: {step}");
            try
            {
                Assert.Contains(expected, actual, step);
            }
            catch (AssertionException)
            {
                TE.FailCurrentStep(expected.ToString(), actual.ToString());
                throw;
            }
        }

        public void IsTrue(bool condition, string step)
        {
              True(condition, step);
        }



        public void Fail(string step)
        {
            TE.BeginTestCaseStep($"Assert: {step}");
            try
            {
                Assert.Fail(step);
            }
            catch (AssertionException)
            {
                TE.FailCurrentStep("true", "false");
                throw;
            }
        }
        public void False(bool result, string step)
        {
            TE.BeginTestCaseStep($"Assert: {step}");
            try
            {
                Assert.False(result, step);
            }
            catch (AssertionException)
            {
                TE.FailCurrentStep("false", "true");
                throw;
            }
        }
        public void Ignore(string step)
        {
            TE.BeginTestCaseStep($"Assert: {step}");
            try
            {
                Assert.Ignore(step);
            }
            catch (AssertionException)
            {
                TE.FailCurrentStep("Test to run", "Test ignored");
                throw;
            }
        }
        public void NotNull(object obj, string step)
        {
            TE.BeginTestCaseStep($"Assert: {step}");
            try
            {
                Assert.NotNull(obj, step);
            }
            catch (AssertionException)
            {
                TE.FailCurrentStep("Not Null", "Object Was Null");
                throw;
            }
        }
        public void Null(object obj, string step)
        {
            TE.BeginTestCaseStep($"Assert: {step}");
            try
            {
                Assert.Null(obj, step);
            }
            catch (AssertionException)
            {
                TE.FailCurrentStep("Object To Be Null", "Was Not Null");
                throw;
            }
        }
        //        public void Pass(string step)
        //        {
        //            TE.BeginTestCaseStep($"Assert: {step}");
        //        }
        public void That(bool result, string step)
        {
            TE.BeginTestCaseStep($"Assert: {step}");
            try
            {
                Assert.That(result, step);
            }
            catch (AssertionException)
            {
                TE.FailCurrentStep("true", "false");
                throw;
            }
        }
        public void That(dynamic first, IResolveConstraint constraint, string message = "")
        {
            TE.BeginTestCaseStep($"Assert: {message}");
            try
            {
                Assert.That(first, constraint, message);
            }
            catch (AssertionException)
            {
                TE.FailCurrentStep("true", "false");
                throw;
            }
        }
        public void True(bool result, string step)
        {
            TE.BeginTestCaseStep($"Assert: {step}");
            try
            {
                Assert.True(result, step);
            }
            catch (AssertionException)
            {
                TE.FailCurrentStep("true", "false");
                throw;
            }
        }

        public void Multiple(TestDelegate testDelegate)
        {
            Assert.Multiple(testDelegate);
        }
    }
}
