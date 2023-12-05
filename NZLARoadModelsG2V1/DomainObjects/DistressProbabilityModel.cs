using DocumentFormat.OpenXml.Spreadsheet;
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
    
    private readonly double coeffFlushing;
    private readonly double coeffScabbing;
    private readonly double coeffLTCracks;    
    private readonly double coeffAlligatorCracks;
    private readonly double coeffShoving;
    private readonly double coeffPotholes;



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

        logit += this.coeffScabbing * segment.PctScabbing;
        logit += this.coeffLTCracks * segment.PctLTCracks;
        logit += this.coeffAlligatorCracks * segment.PctAlligatorCracks;
        logit += this.coeffShoving * segment.PctShoving;
        
        //convert logit to a probability
        double proba = Math.Exp(logit) / (1 + Math.Exp(logit));
        return proba;
    }



}
