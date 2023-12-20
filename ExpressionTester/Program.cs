// See https://aka.ms/new-console-template for more information


using JCass_Data.Utils;
using TestHarness;

string workFolder = @"C:\Users\fritz\Juno Services Dropbox\Local_Authorities\aa_gen2_models\model_development\functions_testing\";
string testFilePath = Path.Combine(workFolder, "ExpressionTest1.xlsx");
TestHarness.FunctionTester tester = new TestHarness.FunctionTester(testFilePath);
tester.RunTest();



