// See https://aka.ms/new-console-template for more information

using TestHarness;

string testFilePath = @"C:\\zz_trash\ExpressionTest1.xlsx";
TestHarness.ExpressionTester tester = new TestHarness.ExpressionTester(testFilePath);
tester.RunTest();



