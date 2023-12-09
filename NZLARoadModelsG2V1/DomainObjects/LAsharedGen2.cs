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

namespace NZLARoadModelsG2V1.DomainObjects;

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

}
