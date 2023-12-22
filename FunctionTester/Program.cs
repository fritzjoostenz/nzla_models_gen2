// See https://aka.ms/new-console-template for more information


using JCass_Data.Utils;
using TestHarness;

//string workFolder = args[0] + @"\"; 
string testFilePath = args[0];  //Path.Combine(workFolder, "ExpressionTest1.xlsx");
TestHarness.FunctionTester tester = new TestHarness.FunctionTester(testFilePath);
tester.RunTest();



