using DocumentFormat.OpenXml.EMMA;
using JCass_ModelCore.ModelObjects;
using Microsoft.ML;
using MLModelClasses.DomainClasses;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NZLARoadModelsG2V1.DomainObjects;

public class RoadModSegmentV1: RoadSegmentBase
{
    
    #region Variables/Properties

    public double AreaM2;

    public string AreaName;

    public double PDI;

    public double SDI;

    public double PeriodsSinceMaintenance;

    public bool IsRehabRoute;

    public bool IsRoundabout;

    public double RemainingStructuralLife;

    public string PavementUse;

    public bool CanTreatCustom;

    public string ONRC;

    public float BusRoutes;

    public float SurfWidth;

    public string SurfMaterial;

    public float SurfLifeExpected;

    public float SurfLifeAchievedPercent 
    {
        get { return (float)(100 * this.SurfAge / this.SurfLifeExpected); }
    }

    public float SurfLifeRemaining
    { 
        get { return this.SurfLifeExpected - this.SurfAge; }
    }

    public float HeavyVehicles
    {
        get { return this.ADT * this.HeavyPercent / 100; }
    }


    /// <summary>
    /// Code for the next surfacing - this should be "ac" or "seal". It can be used to check if any future surfacings
    /// should change type, e.g. if the policy is to downgrade "ac" to "seal" under some situations. This flag should
    /// be determined in the pre-processing based on the client policy
    /// </summary>
    public string NextSurfacing;

    /// <summary>
    /// Normalised structural deficit score
    /// </summary>
    public double StructuralDeficit;


    public double FlushingTDI;
    public double ScabbingTDI;
    public double LTCracksTDI;
    public double MeshCracksTDI;
    public double ShovingTDI;
    public double PotholesTDI;


    #endregion

    #region Constructor and Getters

    public static RoadModSegmentV1 LoadFromRawData(ModelBase model, string[] rawRow)
    {
        RoadModSegmentV1 obs = new RoadModSegmentV1
        {

            StructuralDeficit = model.GetRawData_Number(rawRow, "struc_deficit"),
            IsRehabRoute = Convert.ToBoolean(model.GetRawData_Text(rawRow, "is_rehab_route")),
            IsRoundabout = Convert.ToBoolean(model.GetRawData_Text(rawRow, "is_roundabout")),
            AreaM2 = model.GetRawData_Number(rawRow, "area_m2"),
            AreaName = model.GetRawData_Text(rawRow, "area_name"),
            Length = (float)model.GetRawData_Number(rawRow, "length"),

            ONRC = model.GetRawData_Text(rawRow, "onrc"),
            UrbanRural = model.GetRawData_Text(rawRow, "urban_rural"),
            PavementUse = model.GetRawData_Text(rawRow, "pave_use"),
            BusRoutes = (float)model.GetRawData_Number(rawRow, "no_of_bus_routes"),

            SurfWidth = (float)model.GetRawData_Number(rawRow, "surf_width"),
            SurfAge = GetAgeFromDate(model.GetRawData_Text(rawRow, "surf_date")),
            SurfClass = model.GetRawData_Text(rawRow, "surf_class"),
            SurfMaterial = model.GetRawData_Text(rawRow, "surf_material"),
            SurfFunction = model.GetRawData_Text(rawRow, "surf_function"),
            SurfLayerCount = (float)model.GetRawData_Number(rawRow, "surf_layer_no"),
            SurfThickness = (float)model.GetRawData_Number(rawRow, "surf_thick"),
            SurfLifeExpected = (float)model.GetRawData_Number(rawRow, "surf_life_expected"),

            PavementAge = GetAgeFromDate(model.GetRawData_Text(rawRow, "pave_date")),

            PctFlushing = (float)model.GetRawData_Number(rawRow, "pct_flush"),
            PctScabbing = (float)model.GetRawData_Number(rawRow, "pct_scabb"),
            PctLTCracks = (float)model.GetRawData_Number(rawRow, "pct_lt_crax"),
            PctAlligatorCracks = (float)model.GetRawData_Number(rawRow, "pct_allig"),
            PctShoving = (float)model.GetRawData_Number(rawRow, "pct_shove"),
            PctPotholes = (float)model.GetRawData_Number(rawRow, "pct_poth"),

            Rut85th = (float)GetInitialRut(model, rawRow),
            Naasra85th = (float)GetInitialNaasra(model, rawRow),

            //For FWP rows, we use the current ADT and HCV
            ADT = (float)model.GetRawData_Number(rawRow, "adt"),
            HeavyPercent = (float)model.GetRawData_Number(rawRow, "heavy_perc"),

            CanTreatCustom = Convert.ToBoolean(model.GetRawData_Text(rawRow, "can_treat")),

            PeriodsSinceMaintenance = 999

        };

        //New property introduced for Tauranga CC (after Timaru). Handle case where this value/column is 
        //not available in the raw data. In that case, the next surfacing is simply the same as the current
        //surfacing class (replace like with like)
        if (model.RawHeaders.ContainsKey("next_surf"))
        {
            obs.NextSurfacing = model.GetRawData_Text(rawRow, "next_surf");
        }
        else
        {
            // No data, so presume replace like with like
            obs.NextSurfacing = obs.SurfClass;
        }

        obs.RemainingStructuralLife = obs.GetMSDRemainingLife(model.GetRawData_Number(rawRow, "msd_remlife_lwp"), model.GetRawData_Number(rawRow, "msd_remlife_rwp"));
        
        return obs;

    }

    public static RoadModSegmentV1 LoadFromModelData(ModelBase model, string[] rawRow, double[] newValues)
    {
        string areaName = model.GetRawData_Text(rawRow, "area_name");

        RoadModSegmentV1 obs = new RoadModSegmentV1
        {
            //IsRoundabout = areaName.ToString().ToLower().Contains("/rnbt"),
            RemainingStructuralLife = (float)newValues[PIndex("par_struc_remlife", model)],
            StructuralDeficit = (float)newValues[PIndex("par_struc_deficit", model)],
            IsRehabRoute = Convert.ToBoolean(model.GetRawData_Text(rawRow, "is_rehab_route")),
            IsRoundabout = Convert.ToBoolean(model.GetRawData_Text(rawRow, "is_roundabout")),
            AreaName = model.GetRawData_Text(rawRow, "area_name"),
            AreaM2 = model.GetRawData_Number(rawRow, "area_m2"),
            Length = (float)model.GetRawData_Number(rawRow, "length"),

            ONRC = model.GetRawData_Text(rawRow, "onrc"),
            UrbanRural = model.GetRawData_Text(rawRow, "urban_rural"),
            PavementUse = model.GetRawData_Text(rawRow, "pave_use"),
            BusRoutes = (float)model.GetRawData_Number(rawRow, "no_of_bus_routes"),

            SurfWidth = (float)model.GetRawData_Number(rawRow, "surf_width"),
            SurfClass = model.GetParamValueToText("par_surf_class", newValues),
            SurfMaterial = model.GetParamValueToText("par_surf_mat", newValues),
            SurfFunction = model.GetParamValueToText("par_surf_func", newValues),
            SurfAge = (float)newValues[PIndex("par_surf_age", model)],
            SurfLayerCount = (float)newValues[PIndex("par_surf_layers", model)],
            SurfThickness = (float)newValues[PIndex("par_surf_thick", model)],
            SurfLifeExpected = (float)newValues[PIndex("par_surf_exp_life", model)],

            PavementAge = (float)newValues[PIndex("par_pav_age", model)],

            Rut85th = (float)newValues[PIndex("par_rut", model)],
            Naasra85th = (float)newValues[PIndex("par_naasra", model)],
            
            ADT = (float)newValues[PIndex("par_adt", model)],            
            HeavyPercent = (float)model.GetRawData_Number(rawRow, "heavy_perc"),

            PctFlushing = Math.Min(4, (float)newValues[PIndex("par_pct_flushing", model)]),
            PctScabbing = Math.Min(4, (float)newValues[PIndex("par_pct_scabbing", model)]),
            PctLTCracks = Math.Min(4, (float)newValues[PIndex("par_pct_lt_cracks", model)]),
            PctAlligatorCracks = Math.Min(4, (float)newValues[PIndex("par_pct_mesh_cracks", model)]),
            PctShoving = Math.Min(4, (float)newValues[PIndex("par_pct_shoving", model)]),
            PctPotholes = Math.Min(4, (float)newValues[PIndex("par_pct_potholes", model)]),
                        
            FlushingTDI = Math.Min(4, (float)newValues[PIndex("par_pct_flushing_tdi", model)]),
            ScabbingTDI = Math.Min(4, (float)newValues[PIndex("par_pct_scabbing_tdi", model)]),
            LTCracksTDI = Math.Min(4, (float)newValues[PIndex("par_pct_lt_cracks_tdi", model)]),
            MeshCracksTDI = Math.Min(4, (float)newValues[PIndex("par_pct_mesh_cracks_tdi", model)]),
            ShovingTDI = Math.Min(4, (float)newValues[PIndex("par_pct_shoving_tdi", model)]),
            PotholesTDI = Math.Min(4, (float)newValues[PIndex("par_pct_potholes_tdi", model)]),

            CanTreatCustom = Convert.ToBoolean(model.GetRawData_Text(rawRow, "can_treat")),

        };

        //Handle case where this value/column is not available in the raw data.
        //In that case, the next surfacing is simply the same as the current
        //surfacing class (replace like with like)
        if (model.RawHeaders.ContainsKey("next_surf"))
        {
            obs.NextSurfacing = model.GetRawData_Text(rawRow, "next_surf");
        }
        else
        {
            // No data, so presume replace like with like
            obs.NextSurfacing = obs.SurfClass;
        }
        
        return obs;
    }

    #endregion

    #region Utilities - General


    protected static int PIndex(string paramName, ModelBase model)
    {
        return model.ParamNames[paramName];
    }

    private static float GetAgeFromDate(string dateAsText)
    {
        CultureInfo provider = CultureInfo.InvariantCulture;
        DateTime date = DateTime.Parse(dateAsText);
        if (date == null) { throw new Exception($"Cannot parse date from value '{dateAsText}'"); }
        TimeSpan elapsed = DateTime.Now - date;
        float yearsElapsed = (float)(elapsed.TotalDays / 364.25);
        return yearsElapsed;
    }

    #endregion

    #region Utilities - Surfacing/Pavement

    public double GetMSDRemainingLife(double lwpRemLife, double rwpRemLife)
    {
        //Both lwp and rwp has values
        if (lwpRemLife >= 0 && rwpRemLife >= 0)
        {
            return Math.Min(lwpRemLife, rwpRemLife);  //return minimum of the two
        }
        else if (lwpRemLife == -1 && rwpRemLife == -1)  //None has values
        {
            return -1;  //Return -1 which indicates no data
        }
        else
        {
            // If we get here, it means only one of lwp or rwp has valid data, find which one
            if (lwpRemLife >= 0)
            {
                return lwpRemLife;
            }
            if (rwpRemLife >= 0)
            {
                return rwpRemLife;
            }
            return -1;  //should not get here
        }
    }

    

    #endregion

    #region Utilities - Treatment Triggering

    public bool IsSecondCoatSituation(ModelBase model)
    {

        if (SurfFunction == "1" && SurfClass == "seal")
        {
            //Get Surfacing age, but add 1 to get the age at the END of THIS year
            double surfAge = Math.Floor(SurfAge + 1);
            double secondCoatWait = model.GetLookupValueNumber("second coat", ONRC);
            if (surfAge >= secondCoatWait)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        return false;
    }

    public bool CanConsiderTreatment(double pdiThreshold, double sdiThreshold, double waitTimeBetweenTreatments)
    {
        if (CanTreatCustom == false) { return false; }

        if (SurfAge > (float)waitTimeBetweenTreatments && (PDI > pdiThreshold || SDI > sdiThreshold))
        {
            return true;
        }
        return false;
    }

    public bool CanConsiderRehab(double minLengthAllowed, double maxLengthAllowed, double pdiThreshold, double msdRemLifeMaxThreshold)
    {
        if (IsRehabRoute)
        {
            //First check for disqualifying conditions
            if (Length < minLengthAllowed) { return false; }
            if (Length > maxLengthAllowed) { return false; }
            if (PDI < pdiThreshold) { return false; }

            //Now check for qualifying conditions based on MSD remaining life - only do rehab if MSD has data (value NOT minus 1) and
            // and the remaining life is below threshold
            if (RemainingStructuralLife < 0) { return true; }  //Do not know remaining life (no MSD or FWD)

            if (RemainingStructuralLife <= msdRemLifeMaxThreshold) { return true; }
            return false;
        }
        else
        {
            return false;
        }
    }

    public bool ConsiderAsphaltTreatment(ModelBase model)
    {
        if (NextSurfacing == "ac" || IsRoundabout)
        {
            return true;
        }
        else
        { return false; }
    }

    #endregion

    #region Distress Related


    public static double GetInitialRut(ModelBase model, string[] rawRow)
    {
        //Initial rut is maximum of LWP and RWP values
        double rut = Math.Max(model.GetRawData_Number(rawRow, "rut_lwpmean_85"), model.GetRawData_Number(rawRow, "rut_rwpmean_85"));
        double percRemaining = 1;
        double minAllowed = model.GetLookupValueNumber("resets", "rut_reset_min");
        double maxAllowed = model.GetLookupValueNumber("resets", "rut_reset_max");

        double surveyAge = GetAgeFromDate(model.GetRawData_Text(rawRow, "hsd_date"));
        double surfAge = GetAgeFromDate(model.GetRawData_Text(rawRow, "surf_date"));
        if (surfAge < surveyAge)
        {
            string surfClass = model.GetRawData_Text(rawRow, "surf_class");

            string surfFunc = model.GetRawData_Text(rawRow, "surf_function");
            if (surfFunc == "1")
            {
                return minAllowed;
            }
            else if (surfClass == "ac")
            {
                percRemaining = model.GetLookupValueNumber("resets", "ac_rut_perc_post");
            }
            else
            {
                percRemaining = model.GetLookupValueNumber("resets", "seal_rut_perc_post");
            }
        }
        return Math.Clamp(rut * percRemaining, minAllowed, maxAllowed);
    }

    public static double GetInitialNaasra(ModelBase model, string[] rawRow)
    {
        double currentValue = model.GetRawData_Number(rawRow, "naasra_85");
        double percRemaining = 1;

        double minAllowed = model.GetLookupValueNumber("resets", "naasra_reset_min");
        double maxAllowed = model.GetLookupValueNumber("resets", "naasra_reset_max");

        double surveyAge = GetAgeFromDate(model.GetRawData_Text(rawRow, "roughsegment_date"));
        double surfAge = GetAgeFromDate(model.GetRawData_Text(rawRow, "surf_date"));

        if (surfAge < surveyAge)
        {
            string surfClass = model.GetRawData_Text(rawRow, "surf_class");

            string surfFunc = model.GetRawData_Text(rawRow, "surf_function");
            if (surfFunc == "1")
            {
                return minAllowed;
            }
            else if (surfClass == "ac")
            {
                percRemaining = model.GetLookupValueNumber("resets", "ac_naasra_perc_post");
            }
            else
            {
                percRemaining = model.GetLookupValueNumber("resets", "seal_naasra_perc_post");
            }
        }
        return Math.Clamp(currentValue * percRemaining, minAllowed, maxAllowed);
    }

    public static double GetInitialDistress(ModelBase model, string[] rawRow, string distressKey)
    {
        double currentValue = model.GetRawData_Number(rawRow, distressKey);
        double percRemaining = 1;
        double surveyAge = GetAgeFromDate(model.GetRawData_Text(rawRow, "cond_survey_date"));
        double surfAge = GetAgeFromDate(model.GetRawData_Text(rawRow, "surf_date"));

        if (surfAge < surveyAge)
        {
            string surfClass = model.GetRawData_Text(rawRow, "surf_class");

            string surfFunc = model.GetRawData_Text(rawRow, "surf_function");
            if (surfFunc == "1")
            {
                return 0;
            }
            else if (surfClass == "ac")
            {
                string key = "ac_" + distressKey + "_reset";
                percRemaining = model.GetLookupValueNumber("resets", key);
            }
            else
            {
                string key = "seal_" + distressKey + "_reset";
                percRemaining = model.GetLookupValueNumber("resets", key);
            }
        }
        double minAllowed = 0;
        double maxAllowed = 4;
        return Math.Clamp(currentValue * percRemaining, minAllowed, maxAllowed);
    }

    public double GetDistressPercentFromParameterCode(string distressParamCode)
    {        
        switch (distressParamCode)
        {
            case "par_pct_flushing": return this.PctFlushing;
            case "par_pct_scabbing": return this.PctScabbing;
            case "par_pct_lt_cracks": return this.PctLTCracks;
            case "par_pct_mesh_cracks": return this.PctAlligatorCracks;
            case "par_pct_shoving": return this.PctShoving;
            case "par_pct_potholes": return this.PctPotholes;            
            default:
                throw new Exception($"Distress code '{distressParamCode}' is not handled.");
        }
    }

    public double GetDistressTDIFromParameterCode(string distressParamCode)
    {
        switch (distressParamCode)
        {
            case "par_pct_flushing": return this.PctFlushing;
            case "par_pct_scabbing": return this.PctScabbing;
            case "par_pct_lt_cracks": return this.PctLTCracks;
            case "par_pct_mesh_cracks": return this.PctAlligatorCracks;
            case "par_pct_shoving": return this.PctShoving;
            case "par_pct_potholes": return this.PctPotholes;
            default:
                throw new Exception($"Distress code '{distressParamCode}' is not handled.");
        }
    }

    #endregion










}
