using DocumentFormat.OpenXml.Spreadsheet;
using JCass_Core.Engineering;
using JCass_Functions.Engineering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NZLARoadModelsG2V1.DomainObjects;

public class DistressProbabilityModel
{
   
    private readonly double coeffIntercept;
    private readonly double coeffSurfClassSeal;
    private readonly double coeffSurfThick;
    private readonly double coeffUrbRuralU;
    private readonly double coeffADTlog;
    private readonly double coeffHeavyPercent;
    private readonly double coeffPavementAge;
    
    private readonly double coeffFlushing = 0;
    private readonly double coeffScabbing = 0;
    private readonly double coeffLTCracks = 0;    
    private readonly double coeffAlligatorCracks = 0;
    private readonly double coeffShoving = 0;
    private readonly double coeffPotholes = 0;

    private readonly double coeffRutting = 0;
    private readonly double coeffRoughness = 0;

    public DistressProbabilityModel(Dictionary<string, object> coefficients)
    {
        this.coeffIntercept = Convert.ToDouble(coefficients["(Intercept)"]);
        this.coeffSurfClassSeal = Convert.ToDouble(coefficients["surf_classseal"]);
        this.coeffSurfThick = Convert.ToDouble(coefficients["surf_thick"]);
        this.coeffUrbRuralU = Convert.ToDouble(coefficients["urban_ruralU"]);
        this.coeffADTlog = Convert.ToDouble(coefficients["log(adt)"]);
        this.coeffHeavyPercent = Convert.ToDouble(coefficients["heavy_perc"]);
        this.coeffPavementAge = Convert.ToDouble(coefficients["pave_age"]);

        this.coeffFlushing = Convert.ToDouble(coefficients["pct_flush"]);
        this.coeffScabbing = Convert.ToDouble(coefficients["pct_scabb"]);
        this.coeffLTCracks = Convert.ToDouble(coefficients["pct_lt_crax"]);
        this.coeffAlligatorCracks = Convert.ToDouble(coefficients["pct_allig"]);
        this.coeffShoving = Convert.ToDouble(coefficients["pct_shove"]);
        this.coeffPotholes = Convert.ToDouble(coefficients["pct_poth"]);

        if (coefficients.ContainsKey("rutting")) { this.coeffRutting = Convert.ToDouble(coefficients["rutting"]); }
        if (coefficients.ContainsKey("naasra_85")) { this.coeffRoughness = Convert.ToDouble(coefficients["naasra_85"]); }
    }

    public double GetProbability(RoadModSegmentV1 segment)
    {
        double logit = this.coeffIntercept;
        if (segment.SurfClass == "seal") { logit += this.coeffSurfClassSeal; }
        logit += this.coeffSurfThick * segment.SurfThickness;
        if (segment.UrbanRural == "U") { logit += this.coeffUrbRuralU; }
        logit += this.coeffADTlog * Math.Log(segment.ADT);
        logit += this.coeffHeavyPercent * segment.HeavyPercent;
        logit += this.coeffPavementAge * segment.PavementAge;

        //"par_pct_flushing", "", "", "", "", "par_pct_potholes"
        logit += this.coeffScabbing * segment.PctScabbing;
        logit += this.coeffLTCracks * segment.PctLTcracks;
        logit += this.coeffAlligatorCracks * segment.PctMeshCracks;
        logit += this.coeffShoving * segment.PctShoving;
        logit += this.coeffPotholes * segment.PctPotholes;
        logit += this.coeffRutting * segment.Rut85th;
        logit += this.coeffRoughness * segment.Naasra85th;
        //convert logit to a probability
        double proba = Math.Exp(logit) / (1 + Math.Exp(logit));
        return proba;
    }

}


public class DistressProgressionModel
{
    
    private readonly string? DistressCode;
    private readonly DistressProbabilityModel probabilityModel;    
    private readonly double T100Min;
    private readonly double T100Max;    
    private readonly double InitValMin;
    private readonly double InitValMax;
    
    public double InitValueExpected;
    public double AADI;
    public double T100;

    public DistressProgressionModel(string distressCode, DistressProbabilityModel probModel, string setupString)
    {
        this.DistressCode = distressCode;
        this.probabilityModel = probModel;

        List<string> values = JCass_Core.Utils.HelperMethods.SplitStringToList(setupString, "|");
        if (values.Count != 5) { throw new Exception($"Incorrect setup string for S-progression model for '{distressCode}'. Expected 5 values but got {values.Count}"); }

        this.InitValueExpected = Convert.ToDouble(values[0]);
        this.InitValMin = Convert.ToDouble(values[1]); ;
        this.InitValMax = Convert.ToDouble(values[2]); ;
        this.T100Min = Convert.ToDouble(values[3]); ;
        this.T100Max = Convert.ToDouble(values[4]); ;
        
        if (this.InitValMax <= this.InitValMin) { throw new Exception($"Initial value maximum must be greater than initial value minimum. Check setup string for S-progression model for '{distressCode}'"); }
        if (this.T100Max <= this.T100Min) { throw new Exception($"T100 maximum must be greater than T100 minimum. Check setup string for S-progression model for '{distressCode}'"); }


    }

    public void Initialise(RoadModSegmentV1 segment, double observedValue)
    {
        double probability = this.probabilityModel.GetProbability(segment);
        this.AADI = segment.SurfLifeExpected * (1 - probability);
        this.T100 = this.T100Max * (1 - probability);        
        SShapedModelHelper.CalibrateFactors(segment.SurfAge, observedValue, this.T100Min, this.T100Max, 1, segment.SurfLifeExpected, this.InitValMin, this.InitValMax, ref this.T100, ref this.AADI, ref this.InitValueExpected );
    }

    public double GetIncrement(double currentAge)
    {        
        if (currentAge < this.AADI) { return 0; }                
        double periodsSinceInitialisation = currentAge - this.AADI;
        double increm = SShapedModelHelper.GetSCurveProgressionIncrement(this.T100, periodsSinceInitialisation);
        return increm;
    }


}
