using JCass_Core.Engineering;
using JCass_Core.MachineLearning;
using JCass_Data.Objects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NZLARoadModelsG2V1.DomainObjects;
using System;
using System.Drawing;

namespace NZLARoadModelsG2V1.UnitTests;

[TestClass]
public class NZLAModelsUnitTests
{
    jcDataSet coefficients;
    jcDataSet testData;

    jcDataSet coefficients_rutrough;
    jcDataSet testData_rutrough;


    string dataFolder = @"C:\Users\fritz\Juno Services Dropbox\Local_Authorities\aa_gen2_models\model_development\data\";

    public NZLAModelsUnitTests()
    {        
        string coeffsFile = Path.Combine(dataFolder, "logistic_regression_coeffs.csv");
        string testDataFile = Path.Combine(dataFolder, "logistic_regression_test_data.csv");
        this.coefficients = JCass_Data.Utils.CSVHelper.ReadDataFromCsvFile(coeffsFile, "distress");
        this.testData = JCass_Data.Utils.CSVHelper.ReadDataFromCsvFile(testDataFile);


        coeffsFile = Path.Combine(dataFolder, "logistic_regression_rut_rough_coeffs.csv");
        testDataFile = Path.Combine(dataFolder, "logistic_regression_rut_rough_test_data.csv");
        this.coefficients_rutrough = JCass_Data.Utils.CSVHelper.ReadDataFromCsvFile(coeffsFile, "distress");
        this.testData_rutrough = JCass_Data.Utils.CSVHelper.ReadDataFromCsvFile(testDataFile);
    }

    [TestMethod]
    public void FlushingProbabilityTest()
    {
        DistressProbabilityModel model = new DistressProbabilityModel(this.coefficients.Row("pct_flush"));
        var testCase = this.GetTestCaseFromTestData(0);        
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));

        testCase = this.GetTestCaseFromTestData(1);
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));

        testCase = this.GetTestCaseFromTestData(2);
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));
    }

    [TestMethod]
    public void ScabbingProbabilityTest()
    {
        DistressProbabilityModel model = new DistressProbabilityModel(this.coefficients.Row("pct_scabb"));
        var testCase = this.GetTestCaseFromTestData(3);
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));

        testCase = this.GetTestCaseFromTestData(4);
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));

        testCase = this.GetTestCaseFromTestData(5);
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));

    }


    [TestMethod]
    public void LTCracksProbabilityTest()
    {
        DistressProbabilityModel model = new DistressProbabilityModel(this.coefficients.Row("pct_lt_crax"));
        var testCase = this.GetTestCaseFromTestData(6);
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));

        testCase = this.GetTestCaseFromTestData(7);
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));

        testCase = this.GetTestCaseFromTestData(8);
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));

    }

    [TestMethod]
    public void AlligatorCracksProbabilityTest()
    {
        DistressProbabilityModel model = new DistressProbabilityModel(this.coefficients.Row("pct_allig"));
        var testCase = this.GetTestCaseFromTestData(9);
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));

        testCase = this.GetTestCaseFromTestData(10);
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));

        testCase = this.GetTestCaseFromTestData(11);
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));

    }

    [TestMethod]
    public void ShovingProbabilityTest()
    {
        DistressProbabilityModel model = new DistressProbabilityModel(this.coefficients.Row("pct_shove"));
        var testCase = this.GetTestCaseFromTestData(12);
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));

        testCase = this.GetTestCaseFromTestData(13);
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));

        testCase = this.GetTestCaseFromTestData(14);
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));

    }

    [TestMethod]
    public void PotholesProbabilityTest()
    {
        DistressProbabilityModel model = new DistressProbabilityModel(this.coefficients.Row("pct_poth"));
        var testCase = this.GetTestCaseFromTestData(15);
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));

        testCase = this.GetTestCaseFromTestData(16);
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));

        testCase = this.GetTestCaseFromTestData(17);
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));

    }

    [TestMethod]
    public void RuttingProbabilityTest()
    {
        DistressProbabilityModel model = new DistressProbabilityModel(this.coefficients_rutrough.Row("rutting"));
        var testCase = this.GetTestCaseFromTestData(0, true);
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));

        testCase = this.GetTestCaseFromTestData(1, true);
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));

        testCase = this.GetTestCaseFromTestData(2, true);
        Assert.AreEqual(testCase.Item2, Math.Round(model.GetProbability(testCase.Item1), 5));

    }

    [TestMethod]
    public void RoughnessProbabilityTest()
    {
        DistressProbabilityModel model = new DistressProbabilityModel(this.coefficients_rutrough.Row("naasra_85"));
        var testCase = this.GetTestCaseFromTestData(3, true);
        Assert.AreEqual(Math.Round(testCase.Item2,3), Math.Round(model.GetProbability(testCase.Item1), 3));

        testCase = this.GetTestCaseFromTestData(4, true);
        Assert.AreEqual(Math.Round(testCase.Item2, 3), Math.Round(model.GetProbability(testCase.Item1), 3));

        testCase = this.GetTestCaseFromTestData(5, true);
        Assert.AreEqual(Math.Round(testCase.Item2, 3), Math.Round(model.GetProbability(testCase.Item1), 3));

    }


    private Tuple<RoadModSegmentV1, double> GetTestCaseFromTestData(int iRow, bool useRutRoughness = false)
    {
        RoadModSegmentV1 seg = new RoadModSegmentV1(1, null, null);
        Dictionary<string, object> row = this.testData.Row(iRow);
        if (useRutRoughness) { row = this.testData_rutrough.Row(iRow); }
        seg.SurfClass = Convert.ToString(row["surf_class"]);
        seg.SurfThickness = Convert.ToSingle(row["surf_thick"]); 
        seg.UrbanRural = Convert.ToString(row["urban_rural"]);
        seg.ADT = Convert.ToSingle(row["adt"]);
        seg.HeavyPercent = Convert.ToSingle(row["heavy_perc"]);
        seg.PavementAge = Convert.ToSingle(row["pave_age"]);

        
        seg.PctFlushing = Convert.ToSingle(row["pct_flush"]);
        seg.PctScabbing = Convert.ToSingle(row["pct_scabb"]);
        seg.PctLTcracks = Convert.ToSingle(row["pct_lt_crax"]);
        seg.PctMeshCracks = Convert.ToSingle(row["pct_allig"]);
        seg.PctShoving = Convert.ToSingle(row["pct_shove"]);
        seg.PctPotholes = Convert.ToSingle(row["pct_poth"]);

        if (row.ContainsKey("rutting")) { seg.Rut85th = Convert.ToSingle(row["rutting"]); }
        if (row.ContainsKey("naasra_85")) { seg.Naasra85th = Convert.ToSingle(row["naasra_85"]); } 

        return new Tuple<RoadModSegmentV1, double>(seg, Math.Round(Convert.ToDouble(row["proba"]),5));

    }

}
