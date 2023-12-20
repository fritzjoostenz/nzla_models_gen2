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

    public List<Dictionary<string, object>> FunctionDefinitions;
    public Dictionary<string, object> TestData;
    public Dictionary<string, Dictionary<string, object>> Lookups;

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
        Console.WriteLine();
        Console.WriteLine("Function test results");
        Console.WriteLine("----------------------------------------------------------------------------------------------");
        Console.WriteLine();

        foreach (string key in this.FunctionSet.Functions.Keys)
        {
            IFunction function = this.FunctionSet.Functions[key];

            if (key == "f_reset_lt_cracks_fully")
            {
                int kk = 9;
            }

            object value = this.FunctionSet.Functions[key].Evaluate(TestData);
            this.TestData[key] = value;
            
             object expected = this.FunctionSet.Functions[key].ExpectedTestValue;

            if (!string.IsNullOrEmpty(function.AssignToKey))
            {
                TestData[function.AssignToKey] = value;
            }

            if (expected is null || string.IsNullOrEmpty(expected.ToString()))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{key} = {this.TestData[key]} (no test value found)");
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
                        Console.WriteLine($"{key} = {this.TestData[key]} (failed - expected: {expected})");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{key} = {this.TestData[key]} (ok)");
                    }

                }
                else
                {
                    if (!String.Equals(value.ToString(), expected.ToString()))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{key} = {this.TestData[key]} (failed - expected: {expected})");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{key} = {this.TestData[key]} (ok)");
                    }
                }
            }

        }

        Console.ResetColor();
        Console.ForegroundColor = col;        
        Console.WriteLine();
        Console.WriteLine("----------------------------------------------------------------------------------------------");
        Console.WriteLine();
        Console.WriteLine("Finished testing functions");

    }

           
    private void ReadTestingData(string testFilePath)
    {
        Console.WriteLine($"Reading functions testing data from '{Path.GetFileName(testFilePath)}'");
        string tmpFilePath = ExcelHelper.GetTempFilePath(testFilePath);
        File.Copy(testFilePath, tmpFilePath);

        jcDataSet tmp1 = ExcelHelper.ReadExcelDataBlock(tmpFilePath, "functions");
        this.FunctionDefinitions = tmp1.GetAsListOfDictionaries();

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

        jcDataSet tmp3 = ExcelHelper.ReadExcelDataBlock(tmpFilePath, "test_values");
        List<Dictionary<string, object>> tmpData = tmp3.GetAsListOfDictionaries();
        this.TestData = new Dictionary<string, object>();
        foreach (var row in tmpData)
        {
            string key = row["parameter"].ToString();
            object value = row["value"];
            this.TestData.Add(key, value);
        }

        File.Delete(tmpFilePath);

    }







}
