using JCass_Data.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JCass_Functions.Engineering;

namespace ExpressionTester.Utilities;

internal static class DistribFunctionExplore
{

    public static void GenerateDistributionData()
    {

        List<Dictionary<string, object>> data = new List<Dictionary<string, object>>();

        Console.WriteLine("starting....");
        Random random = new Random();
        int n = 10000;
        var modA = JCass_Functions.Engineering.Utilities.GetSkewedDistribModel_A(0.3);
        var modB = JCass_Functions.Engineering.Utilities.GetSkewedDistribModel_B(0.3);
        var modC = JCass_Functions.Engineering.Utilities.GetSkewedDistribModel_C(0.3);
        var modD = JCass_Functions.Engineering.Utilities.GetSkewedDistribModel_D(0.3);
        for (int i = 0; i < n; i++)
        {
            double t = random.Next(0, 10);
            double a = modA.GetValue(random.NextDouble());
            double b = modB.GetValue(random.NextDouble());
            double c = modC.GetValue(random.NextDouble());
            double d = modD.GetValue(random.NextDouble());

            Dictionary<string, object> row = new Dictionary<string, object>();
            row.Add("index", i);
            row.Add("model_a", a);
            row.Add("model_b", b);
            row.Add("model_c", c);
            row.Add("model_d", d);
            data.Add(row);
        }

        Console.WriteLine("writing file....");
        CSVHelper.ExportToCsv(data, @"C:\\zz_trash\distrib_data.csv");
        Console.WriteLine("done!");

    }

}
