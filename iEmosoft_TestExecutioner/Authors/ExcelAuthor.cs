﻿/*
namespace aUI.Automation.Authors
{
	public class ExcelAuthor : BaseAuthor, IDisposable
	{
	    private int currentStepIndexWrittenToFile = 0;

        Excel.Application excelApp = new Excel.Application();
		Excel.Workbook workbook = null;
		Excel.Worksheet activateWorksheet;
        
		object MISSING = System.Reflection.Missing.Value;
        		
		int RED;
		int GREEN;
		int ORANGE;
		int SALMON;
		int DARK_RED;
		int DARK_GREEN;
        
        public ExcelAuthor(string rootTestCasesFolderOrAppSettingName)
		{
			RED = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Red);
			GREEN = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LimeGreen);
			ORANGE = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Orange);
			SALMON = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Tan);
			DARK_RED = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Brown);
			DARK_GREEN = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Green);

            try
            {
                base.rootTestCasesFolder =
                    System.Configuration.ConfigurationManager.AppSettings[rootTestCasesFolder].ToString();
            }
            catch
            {
                base.rootTestCasesFolder = rootTestCasesFolderOrAppSettingName;
            }
            testCaseTemplatePath = string.Format("{0}\\Resources\\TestCaseTemplate.xlsx", AppDomain.CurrentDomain.BaseDirectory);
		}
       
       	public override bool StartNewTestCase(TestCaseHeaderData testCaseHeader)
       	{
            SaveReport();
            Dispose();

       	    bool result = base.InitialzieNewTestCase(testCaseHeader);

       	    if (result)
       	    {
                //newTestCasePath gets initialized based on the testCaseHeader parameter
       	        base.newTestCasePath += "\\" + testCaseHeader.TestCaseFileName.Replace(".xlsx", "").Replace(".", "") + ".xlsx";
       	    }

            currentStepIndexWrittenToFile = 0;
            return result;
       	}
        
        public override string SaveReport()
        {
            string result = "";

            if (base.fileIsDirty)
            {
                WriteTestCaseHeaderToExcelDocument();
                WriteStepsToExcel();
                UpdatePassFailStatusForWholeTest();
                result = SaveExcelFileToDisk();
                base.fileIsDirty = false;
            }

            return result;
        }

	    public void Dispose()
	    {
	        try
	        {
	            if (activateWorksheet != null)
	            {
	                Marshal.FinalReleaseComObject(activateWorksheet);
	                activateWorksheet = null;
	            }
	        }
	        catch
	        {
	        }

	        try
	        {
	            if (workbook != null)
	            {
	                Marshal.FinalReleaseComObject(workbook);
	                workbook = null;
	            }
	        }
	        catch
	        {
	        }

	        try
	        {
	            if (excelApp != null)
	            {
	                Marshal.FinalReleaseComObject(excelApp);
	                excelApp = null;
	            }
	        }catch {}
	    }
	
	    private void WriteTestCaseHeaderToExcelDocument()
	    {
            //If the workbook is not null, then we've already written the header
	        if (workbook == null)
	        {
	            workbook = excelApp.Workbooks.Open(testCaseTemplatePath, MISSING, MISSING, MISSING, MISSING,
	                MISSING, MISSING, MISSING, MISSING, MISSING, MISSING, MISSING, MISSING, MISSING, MISSING);
	            activateWorksheet = workbook.ActiveSheet;


	            if (!string.IsNullOrEmpty(testCaseHeader.TestNumber))
	            {
	                WriteToExcelFile("A1", testCaseHeader.TestNumber.ToString());
	            }

	            WriteToExcelFile("B2", testCaseHeader.Prereqs);
	            WriteToExcelFile("B3", testCaseHeader.TestName);
	            WriteToExcelFile("B4", testCaseHeader.Priority);
	            WriteToExcelFile("B5", testCaseHeader.TestWriter);
	            WriteToExcelFile("D4", testCaseHeader.ExecutedByName);
	            WriteToExcelFile("D5", testCaseHeader.ExecutedOnDate);
	            WriteToExcelFile("A8", testCaseHeader.TestDescription);
	        }
	    }

	    private void WriteStepsToExcel()
	    {
	        for (int i=currentStepIndexWrittenToFile; i< recordedSteps.Count; i++)
	        {
	            WriteStepToExcel(recordedSteps[i]);
	        }    
	    }

        private void WriteStepToExcel(TestCaseStep step)
		{
           	if (!templateWasFound)
			{
				return;
			}

			//The steps will be number by 10's, this will allow a person to manually insert line items between numbers.
			string stepNumber = ((currentStepIndexWrittenToFile + 1) * 10).ToString();

			//steps being on row #14 in the test case template, as the currentStepIndex increases, so should the row we write too.
			string stepRow = (14 + currentStepIndexWrittenToFile).ToString();
			currentStepIndexWrittenToFile += 1;

			WriteToExcelFile("A" + stepRow, stepNumber);
			WriteToExcelFile("B" + stepRow, step.StepDescription);
			WriteToExcelFile("C" + stepRow, step.SuppliedData);
			WriteToExcelFile("D" + stepRow, step.ExpectedResult);
			WriteToExcelFile("E" + stepRow, step.ActualResult);
			WriteToExcelFile("F" + stepRow, step.StepPassed ? "True" : "FALSE!");
            WriteToExcelFile("H" + stepRow, step.Notes);

            if (!string.IsNullOrEmpty(step.ImageFilePath))
            {
                var range = activateWorksheet.Range["G" + stepRow];
                var hyperLink = activateWorksheet.Hyperlinks.Add(range, step.ImageFilePath, MISSING, MISSING, "Image");
            }

			string statusRow = "F" + stepRow;

			if (!step.StepPassed)
			{
				SetCellsBackColor(statusRow, RED);
			}
			else
			{
				SetCellsBackColor(statusRow, GREEN);
			}

			fileIsDirty = true;
            currentTestCaseStep = null;
		}

	    private string SaveExcelFileToDisk()
	    {
            string newFileName = GetNextFileName();
            workbook.SaveAs(newFileName, MISSING, MISSING, MISSING, MISSING, MISSING, Excel.XlSaveAsAccessMode.xlExclusive, 2, MISSING, MISSING, MISSING, MISSING);
            return newFileName;
        }

        private void UpdatePassFailStatusForWholeTest(){
	        if (fileIsDirty && templateWasFound)
            {
                int darkColor = TestCaseFailed ? DARK_RED : DARK_GREEN;
                int lightColor = TestCaseFailed ? RED : GREEN;
                string passFailText = TestCaseFailed ? "FAIL" : "PASSED";

                WriteToExcelFile("B1", passFailText);
                SetCellsBackColor("B1", lightColor);
                SetCellsBackColor("A1", darkColor);
           }

            fileIsDirty = false;
	    }
       
        private void WriteToExcelFile(string cell, string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                activateWorksheet.get_Range(cell, MISSING).Value = text;
            }
        }

        private void SetCellsBackColor(string cell, int color)
        {
            activateWorksheet.get_Range(cell).Interior.Color = color;
        }
    }
}
*/