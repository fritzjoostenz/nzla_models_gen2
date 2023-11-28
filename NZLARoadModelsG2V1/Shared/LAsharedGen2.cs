using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using JCass_Core.Utils;
using JCass_ModelCore.ModelObjects;

namespace NZLARoadModelsG2V1.Shared;

internal class LAsharedGen2
{

    public static string GetIndexCalcMethodSafe(string rawValue, string errorLabel)
    {
        if (string.IsNullOrEmpty(rawValue)) { throw new Exception($"Null {errorLabel} calculation method specified. Check lookups;"); }
        List<string> indexCalcMethods = new List<string>() { "cost354", "cost_354", "cost 354", "weighted sum", "weighted_sum", "weighted" };
        if (indexCalcMethods.Contains(rawValue) == false) { throw new Exception($"Invalid {errorLabel} calculation method specified. Check lookups;"); }
        if (rawValue.ToLower().StartsWith("cost"))
        {
            return "cost354";
        }
        else
        {
            return "weighted_sum";
        }
    }

    public static string GetRoadCategory(ModelBase model, string[] rawRow, double adt)
    {
        string roadUse = model.GetRawData_Text(rawRow, "road_use");
        string onrc = model.GetRawData_Text(rawRow, "onrc");

        double adtLimit1 = 200;
        double adtLimit2 = 10000;

        if (roadUse == "Urban Industrial" || roadUse == "Urban Commercial")
        {
            return "1A";
        }
        else if (roadUse == "CBD")
        {
            return "1B";
        }
        else if (onrc == "National" || onrc == "Arterial")
        {
            return "2";
        }
        else if ((onrc == "Primary Collector" || onrc == "secondary collector") &
                 adt > adtLimit2)
        {
            return "3";
        }
        else if (adt > adtLimit1 & adt <= adtLimit2)
        {
            return "4";
        }
        else
        {
            return "5";
        }

    }

    public static double GetPDI(ModelBase model, double[] values, double[] weights, string calcMethod)
    {
        if (calcMethod == "cost354")
        {
            return GetPDI_COST354(model, values, weights);
        }
        else
        {
            return GetPDI_WeightedSum(model, values, weights);
        }
    }

    private static double GetPDI_WeightedSum(ModelBase model, double[] values, double[] weights)
    {
        double[] defects = new double[4];
        defects[0] = model.GetParameterValue("par_pct_lt_cracks", values);
        defects[1] = model.GetParameterValue("par_pct_mesh_cracks", values);
        defects[2] = model.GetParameterValue("par_pct_shoving", values);
        defects[3] = model.GetParameterValue("par_pct_potholes", values);

        double dotProduct = defects.Zip(weights, (a, b) => a * b).Sum();
        return dotProduct;
    }

    private static double GetPDI_COST354(ModelBase model, double[] values, double[] weights)
    {
        double[] defects = new double[4];
        defects[0] = model.GetParameterValue("par_pct_lt_cracks", values);
        defects[1] = model.GetParameterValue("par_pct_mesh_cracks", values);
        defects[2] = model.GetParameterValue("par_pctshoving", values);
        defects[3] = model.GetParameterValue("par_pct_potholes", values);

        return JCass_Core.Engineering.IndexCalculator.GetCOST354Index(defects, weights, 4, 20);
    }

    public static double GetSDI(ModelBase model, double[] values, double[] weights, string calcMethod)
    {
        if (calcMethod == "cost354")
        {
            return GetSDI_Cost354(model, values, weights);
        }
        else
        {
            return GetSDI_WeightedSum(model, values, weights);
        }
    }

    private static double GetSDI_WeightedSum(ModelBase model, double[] values, double[] weights)
    {
        double[] defects = new double[4];
        defects[0] = model.GetParameterValue("par_pct_flushing", values);
        defects[1] = model.GetParameterValue("par_pct_scabbing", values);
        defects[2] = model.GetParameterValue("par_pct_mesh_cracks", values);
        defects[3] = model.GetParameterValue("par_pct_potholes", values);

        double dotProduct = defects.Zip(weights, (a, b) => a * b).Sum();
        return dotProduct;
    }

    private static double GetSDI_Cost354(ModelBase model, double[] values, double[] weights)
    {
        double[] defects = new double[4];
        defects[0] = model.GetParameterValue("par_pct_flushing", values);
        defects[1] = model.GetParameterValue("par_pct_scabbing", values);
        defects[2] = model.GetParameterValue("par_pct_mesh_cracks", values);
        defects[3] = model.GetParameterValue("par_pct_potholes", values);

        return JCass_Core.Engineering.IndexCalculator.GetCOST354Index(defects, weights, 4, 20);
    }

    public static double GetObjective_WeightedSum(double pdi, double sdi, double rutIndex, double[] weights)
    {
        double[] defects = new double[3] { pdi, sdi, rutIndex };
        double dotProduct = defects.Zip(weights, (a, b) => a * b).Sum();
        return dotProduct;
    }

    public static double GetObjective_COST354(double pdi, double sdi, double rutIndex, double[] weights)
    {
        double[] defects = new double[3] { pdi, sdi, rutIndex };
        return JCass_Core.Engineering.IndexCalculator.GetCOST354Index(defects, weights, 4, 20);
    }

    public static double GetObjective_COST354(double pdi, double sdi, double rutIndex, double strucDeficit, double[] weights)
    {
        if (strucDeficit < 0)
        {
            double[] defects = new double[3] { pdi, sdi, rutIndex };
            double[] tmp_weights1 = new double[3] { weights[0], weights[1], weights[2] };
            double[] tmp_weights2 = HelperMethods.NormaliseWeights(tmp_weights1);
            return JCass_Core.Engineering.IndexCalculator.GetCOST354Index(defects, tmp_weights2, 4, 20);
        }
        else
        {
            double[] defects = new double[4] { pdi, sdi, rutIndex, strucDeficit };
            return JCass_Core.Engineering.IndexCalculator.GetCOST354Index(defects, weights, 4, 20);
        }

    }

    public static double CanConsiderAsphaltOverlay(double yMaxValue, double sciValue, double yMaxThreshold, double sciThreshold)
    {
        if (yMaxValue > yMaxThreshold) { return 0; }
        if (sciValue > sciThreshold) { return 0; }
        return 1;
    }
}
