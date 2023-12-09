using DocumentFormat.OpenXml.Presentation;
using ExpressionTester.TestHarness;
using JCass_Core.Expressions;
using JCass_Data.Objects;
using JCass_Data.Utils;
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

    private ExpressionSet ExpressionSet;
    

    public ExpressionTester(string setupFile)
    {
        Console.WriteLine($"Reading expressions testing data from '{setupFile}'");
            
        this.DataMan = new ExpressionTestData(setupFile);

        this.values = new Dictionary<string, object>();
        foreach (var newPair in this.DataMan.RawData) values.Add(newPair.Key, newPair.Value);
        foreach (var newPair in this.DataMan.ParameterData) values.Add(newPair.Key, newPair.Value);
                
        this.ExpressionSet = new ExpressionSet();
        this.ExpressionSet.Setup(this.DataMan.ExpressionsData);
        Console.WriteLine($"Finished setting up. {this.ExpressionSet.Expressions.Count} expressions loaded");
    }

    public void RunTest()
    {
        this.ExpressionSet.Calibrate(this.values);
        this.ExpressionSet.Evaluate(this.values);

        var col = Console.ForegroundColor;
        Console.WriteLine();
        Console.WriteLine("Expression test results");
        Console.WriteLine("----------------------------------------------------------------------------------------------");
        Console.WriteLine();

        foreach (string key in this.ExpressionSet.Expressions.Keys)
        {
            

            object expected = this.ExpressionSet.Expressions[key].ExpectedTestValue;
            object value = this.values[key];

            if(HelperMethods.IsNumeric(expected.ToString()) && HelperMethods.IsNumeric(value.ToString()))
            {
                expected = Convert.ToDouble(expected);
                value = Convert.ToDouble(value);

                if (Math.Round((double)expected,2) !=  Math.Round((double)value,2))
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

        Console.ForegroundColor = col;
        Console.WriteLine();
        Console.WriteLine("----------------------------------------------------------------------------------------------");
        Console.WriteLine();
        Console.WriteLine("Finished testing expressions");

    }



    






}
