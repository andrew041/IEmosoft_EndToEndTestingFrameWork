﻿namespace aUI.Automation.ModelObjects
{
    public class TestCaseStep
    {
        public string StepDescription { get; set; } = "";
        public string SuppliedData { get; set; } = "";
        public string ExpectedResult { get; set; } = "";
        public string ActualResult { get; set; } = "";
        public bool StepPassed { get; set; }
        public string Notes { get; set; } = "";
        public string ImageFilePath { get; set; } = "";
        public byte[] ImageData { get; set; } = null;
    }
}
