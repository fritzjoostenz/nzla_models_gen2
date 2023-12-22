using DocumentFormat.OpenXml.Presentation;
using JCass_Data.Objects;
using JCass_Data.Utils;
using JCass_Functions;
using JCass_Functions.BaseClasses;
using JCass_Functions.Equalities;
using NPOI.OpenXmlFormats.Shared;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestHarness;

public class FunctionTester
{
           
    private FunctionSet FunctionSet;

    private Dictionary<string, string> TestCases;
    public List<Dictionary<string, object>> FunctionDefinitions;
    public Dictionary<string, Dictionary<string, object>> TestData;
    public Dictionary<string, Dictionary<string, object>> Lookups;
    public Dictionary<string, Dictionary<string, object>> ExpectedValues;

    public FunctionTester(string testFilePath)
    {
        Console.ResetColor();
        this.ReadTestingData(testFilePath);

        this.FunctionSet = new FunctionSet();
        this.FunctionSet.Setup(this.FunctionDefinitions, this.Lookups);
        Console.WriteLine($"Finished setting up. {this.FunctionSet.Functions.Count} functions loaded");
    }

    public void RunTest()
    {
        var col = Console.ForegroundColor;

        foreach (string testCaseKey in this.TestCases.Keys)
        {

            Console.ResetColor();
            Console.ForegroundColor = col;
            Console.WriteLine();
            Console.WriteLine($"Running tests for test case '{testCaseKey}'");
            Console.WriteLine("----------------------------------------------------------------------------------------------");            
            Console.WriteLine();

            Dictionary<string, object> testCaseData = this.TestData[testCaseKey];
            string evaluationStage = this.TestCases[testCaseKey];

            int iFails = 0;
            int iNoTests = 0;
            int iMatches = 0;
            foreach (string key in this.FunctionSet.Functions.Keys)
            {                
                IFunction function = this.FunctionSet.Functions[key];
                
                if (function.EvaluationStage == "all" || function.EvaluationStage == evaluationStage)
                {

                    object value = this.FunctionSet.Functions[key].Evaluate(testCaseData);
                    testCaseData[key] = value;

                    object expected = this.ExpectedValues[testCaseKey][function.Key];

                    if (!string.IsNullOrEmpty(function.AssignToKey))
                    {
                        testCaseData[function.AssignToKey] = value;
                    }

                    if (expected is null || string.IsNullOrEmpty(expected.ToString()))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"{key} = {this.TestData[testCaseKey][key]} (no test value found)");
                        iNoTests++;
                    }
                    else
                    {
                        if (HelperMethods.IsNumeric(expected.ToString()) && HelperMethods.IsNumeric(value.ToString()))
                        {
                            expected = Convert.ToDouble(expected);
                            value = Convert.ToDouble(value);

                            if (Math.Round((double)expected, 2) != Math.Round((double)value, 2))
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"{key} = {this.TestData[testCaseKey][key]} (failed - expected: {expected})");
                                iFails++;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"{key} = {this.TestData[testCaseKey][key]} (ok)");
                                iMatches++;
                            }

                        }
                        else
                        {
                            if (!String.Equals(value.ToString(), expected.ToString()))
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"{key} = {this.TestData[testCaseKey][key]} (failed - expected: {expected})");
                                iFails++;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"{key} = {this.TestData[testCaseKey][key]} (ok)");
                                iMatches++;
                            }
                        }
                    }

                }

            }

            Console.ResetColor();
            Console.ForegroundColor = col;            
            Console.WriteLine();
            Console.WriteLine($"Summary for '{testCaseKey}'");
            Console.WriteLine($"Test Passed = '{iMatches}'");
            Console.WriteLine($"Test Failed = '{iFails}'");
            Console.WriteLine($"Functions Not Tested = '{iNoTests}'");
            Console.WriteLine();
            Console.WriteLine($"Finished tests for test case '{testCaseKey}'");
            Console.WriteLine("----------------------------------------------------------------------------------------------");
            Console.WriteLine("----------------------------------------------------------------------------------------------");
            Console.WriteLine();
            Console.ResetColor();
            Console.ForegroundColor = col;
        }

    }

           
    private void ReadTestingData(string testFilePath)
    {
        Console.WriteLine($"Reading functions testing data from '{Path.GetFileName(testFilePath)}'");
        string tmpFilePath = ExcelHelper.GetTempFilePath(testFilePath);
        File.Copy(testFilePath, tmpFilePath);

        jcDataSet tmp1 = ExcelHelper.ReadExcelDataBlock(tmpFilePath, "functions");
        var functionDefs = tmp1.GetAsListOfDictionaries();
        this.FunctionDefinitions = functionDefs;

        jcDataSet tmp2 = ExcelHelper.ReadExcelDataBlock(tmpFilePath, "lookups");
        List<Dictionary<string, object>> lkps = tmp2.GetAsListOfDictionaries();
        this.Lookups = new Dictionary<string, Dictionary<string, object>>();
        foreach (var row in lkps)
        {
            string key = row["lookup_set_name"].ToString();
            if (!this.Lookups.ContainsKey(key)) this.Lookups.Add(key, new Dictionary<string, object>());

            string settingKey = row["setting_key"].ToString();
            object settingValue = row["setting_value"];
            this.Lookups[key].Add(settingKey, settingValue);
        }


        this.SetupTestCases(testFilePath);

        jcDataSet tmp3 = ExcelHelper.ReadExcelDataBlock(tmpFilePath, "test_values");
        List<Dictionary<string, object>> tmpData = tmp3.GetAsListOfDictionaries();
        
        this.TestData = new Dictionary<string, Dictionary<string, object>>();
        this.ExpectedValues = new Dictionary<string, Dictionary<string, object>>();
        foreach (string testCaseKey in this.TestCases.Keys)
        {
            if (!this.TestData.Keys.Contains(testCaseKey)) this.TestData.Add(testCaseKey, new Dictionary<string, object>());            
            foreach (var row in tmpData)
            {
                string key = row["parameter"].ToString();
                object value = row[testCaseKey];
                this.TestData[testCaseKey].Add(key, value);
            }

            if (!this.ExpectedValues.Keys.Contains(testCaseKey)) this.ExpectedValues.Add(testCaseKey, new Dictionary<string, object>());
            foreach (var row in functionDefs)
            {
                string key = row["function_key"].ToString();
                object expectedValue = row[testCaseKey];
                this.ExpectedValues[testCaseKey].Add(key, expectedValue);
            }

        }
        
        File.Delete(tmpFilePath);

    }

    private void SetupTestCases(string tmpFilePath)
    {
        jcDataSet tmp1 = ExcelHelper.ReadExcelDataBlock(tmpFilePath, "test_cases");
        this.TestCases = new Dictionary<string, string>();
        for (int i = 0; i < tmp1.Count; i++)
        {
            var row = tmp1.Row(i);
            string testCaseKey = row["test_case_key"].ToString().Trim();
            string evalStage = row["evaluation_stage"].ToString().Trim();
            if (this.TestCases.ContainsKey(testCaseKey)) throw new Exception($"Duplicate test case key '{testCaseKey}'.");
            this.TestCases.Add(testCaseKey, evalStage);
        }
    }







}
