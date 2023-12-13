using DocumentFormat.OpenXml.Presentation;
using ExpressionTester.TestHarness;
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

public class ExpressionTester
{

    private ExpressionTestData DataMan;
    
    private Dictionary<string, object> values;

    private FunctionSet ExpressionSet;
    

    public ExpressionTester(string setupFile)
    {
        Console.ResetColor();
        Console.WriteLine($"Reading expressions testing data from '{setupFile}'");
            
        this.DataMan = new ExpressionTestData(setupFile);

        this.values = new Dictionary<string, object>();
        foreach (var newPair in this.DataMan.RawData) values.Add(newPair.Key, newPair.Value);
        foreach (var newPair in this.DataMan.ParameterData) values.Add(newPair.Key, newPair.Value);
                
        this.ExpressionSet = new FunctionSet();
        this.ExpressionSet.Setup(this.DataMan.ExpressionsData.GetAsListOfDictionaries(), this.DataMan.Lookups);
        Console.WriteLine($"Finished setting up. {this.ExpressionSet.Functions.Count} expressions loaded");
    }

    public void RunTest()
    {                
        var col = Console.ForegroundColor;
        Console.WriteLine();
        Console.WriteLine("Function test results");
        Console.WriteLine("----------------------------------------------------------------------------------------------");
        Console.WriteLine();

        foreach (string key in this.ExpressionSet.Functions.Keys)
        {
            IFunction function = this.ExpressionSet.Functions[key];
            object value = this.ExpressionSet.Functions[key].Evaluate(values);
            this.values[key] = value;

            if (key == "f_flush_probability")
            {
                int kk = 9;
            }

             object expected = this.ExpressionSet.Functions[key].ExpectedTestValue;

            if (!string.IsNullOrEmpty(function.AssignToKey))
            {
                values[function.AssignToKey] = value;
            }

            if (expected is null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{key} = {this.values[key]} (no test value found)");
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
                        Console.WriteLine($"{key} = {this.values[key]} (failed - expected: {expected})");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{key} = {this.values[key]} (ok)");
                    }

                }
                else
                {
                    if (!String.Equals(value.ToString(), expected.ToString()))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{key} = {this.values[key]} (failed - expected: {expected})");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{key} = {this.values[key]} (ok)");
                    }
                }
            }                                
        }

        Console.ResetColor();
        Console.ForegroundColor = col;        
        Console.WriteLine();
        Console.WriteLine("----------------------------------------------------------------------------------------------");
        Console.WriteLine();
        Console.WriteLine("Finished testing expressions");

    }



    






}
