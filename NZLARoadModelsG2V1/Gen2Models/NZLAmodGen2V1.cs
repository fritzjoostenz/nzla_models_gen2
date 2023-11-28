using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;
using JCass_Core.Utils;
using JCass_ModelCore.ModelObjects;
using JCass_ModelCore.Treatments;

using Microsoft.ML;
using NPOI.SS.Formula.Functions;
using SixLabors.ImageSharp.ColorSpaces;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Presentation;
using JCass_ModelCore.Utilities;
using DocumentFormat.OpenXml.Wordprocessing;
using NPOI.HSSF.Record.CF;
using System.Runtime.CompilerServices;
using NPOI.SS.Formula.PTG;
using NPOI.POIFS.Crypt.Dsig.Facets;
using JCass_ModelCore.Customiser;
using NZLARoadModelsG2V1.DomainObjects;
using MLClasses;
using NZLARoadModelsG2V1.Shared;

namespace JCass_CustomiserSample.Gen2;

/// <summary>
/// Second Generation Model, First Version for New Zealand Local Authorities
/// Simplified model that models distress percentages rather than normalised FM distresses
/// </summary>
public class NZLAmodGen2V1 : CustomiserBase, ICustomiser
{

    #region Variables

    private MLContext mlContext;

    private List<string> distressParams = new List<string>() { "par_pct_flushing", "par_pct_scabbing", "par_pct_lt_cracks", "par_pct_mesh_cracks", "par_pct_shoving", "par_pct_potholes" };

    PieceWiseLinearModel rutIncremModel;
    PieceWiseLinearModel naasraIncremModel;

    PieceWiseLinearModel rutToIndexConversionModel;

    private Dictionary<string, PredictionEngine<RoadModSegment, Prediction_Binary>> PredictionModels;

    //private PredictionEngine<RoadModSegment, Prediction_Number> SurfLifeExpectedModel;
    private Dictionary<string, double> SurfLifeExpLookup;

    private PredictionEngine<RoadModSegment, Prediction_Binary> RutRiskModel;
    private PredictionEngine<RoadModSegment, Prediction_Binary> NaasraRiskModel;
    private PredictionEngine<RoadModSegment, Prediction_Binary> MaintRiskModelPA;
    private PredictionEngine<RoadModSegment, Prediction_Binary> MaintRiskModelSU;
    private PredictionEngine<RoadModSegment, Prediction_Number> SurfCondIndexModel;

    #endregion

    #region Lookup Constants

    private string pdiCalcMethod;
    private string sdiCalcMethod;

    private double[] weightsPDI;            //Weights for calculating PDI as weighted sum
    private double[] weightSDI;             //Weights for calculating SDI as weighted sum
    private double[] weightsObjective;       //Weights for calculating Objective Function Value as weighted sum

    private float TrafficGrowthRate;

    private double NaasraMin;
    private double NaasraMax;

    private double RutMin;
    private double RutMax;

    private double WaitTimeBetweenTreatments;
    private double GapToNextTreatment;
    private double PDI_threshold_entry;
    private double SDI_threshold_entry;
    private double PDI_threshold_rehab_ac;
    private double PDI_threshold_rehab_chip;

    private double PDI_threshold_maint;
    private double SDI_threshold_maint;

    private double AsphaltYMaxThreshold;
    private double AsphaltSCIThreshold;

    private double RehabMinLength;
    private double RehabMaxLength;

    private double MSDRemLifeThresholdRehab;

    private double ResetRemLifeAfterRehab;

    private bool IncludePavUseInSurfLifeExpKey = true;

    private bool ScaleObjectiveByLength = false;

    private Dictionary<string, PieceWiseLinearModel> DistressProgressionModels;

    #endregion

    #region Constructor/Setup

    public NZLAmodGen2V1()
    {


    }

    public override void SetupInstance()
    {

        mlContext = new MLContext(model.RandomSeed);

        SetupMLModels();

        DistressProgressionModels = new Dictionary<string, PieceWiseLinearModel>();
        foreach (string distress in distressParams)
        {
            string key1 = distress + "_k1";
            string key2 = distress + "_k2";
            double cal_fact1 = model.GetLookupValueNumber("increments", key1);
            double cal_fact2 = model.GetLookupValueNumber("increments", key2);

            List<double> x_distress = new List<double> { 0, cal_fact1, 4.0 };
            List<double> y_distress = new List<double> { 0, cal_fact2, 0 };
            DistressProgressionModels.Add(distress, new PieceWiseLinearModel(x_distress, y_distress, false));
        }

        double rutCentre = model.GetLookupValueNumber("increments", "rut_increm_central_tendency");
        rutIncremModel = Utils.GetSkewedDistribModel_A(rutCentre);

        double naasraCentre = model.GetLookupValueNumber("increments", "naasra_increm_central_tendency");
        naasraIncremModel = Utils.GetSkewedDistribModel_A(naasraCentre);

        TrafficGrowthRate = Convert.ToSingle(model.Lookups["traffic"]["adt_growth_rate_perc"]);

        NaasraMin = Convert.ToDouble(model.Lookups["resets"]["naasra_reset_min"]);
        NaasraMax = Convert.ToDouble(model.Lookups["resets"]["naasra_reset_max"]);

        RutMin = Convert.ToDouble(model.Lookups["resets"]["rut_reset_min"]);
        RutMax = Convert.ToDouble(model.Lookups["resets"]["rut_reset_max"]);

        ResetRemLifeAfterRehab = Convert.ToDouble(model.Lookups["resets"]["rehab_remlife_reset"]);

        // Piece wise linear model to convert absolute rut depth (in mm) to a distres index scale 0 to 4
        // The X-values are the percentiles same as was used to calculate index values for distresses
        List<double> x_rut = new List<double> { 3.65, 4.05, 5, 6.55, 7.85, 9.65, 14, 19.688, 39.5 };
        List<double> y_index = new List<double> { 0, 0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4 };
        rutToIndexConversionModel = new PieceWiseLinearModel(x_rut, y_index, false);

        //Set up weights for calculating Weighted Sum as PDI.
        // Important: order of weights must match order of distresses. See LAShared.GetPDI() method.
        weightsPDI = new double[4] {
            Convert.ToDouble(model.Lookups["indexes"]["pdi_weight_lt_cracks"]),
            Convert.ToDouble(model.Lookups["indexes"]["pdi_weight_mesh_cracks"]),
            Convert.ToDouble(model.Lookups["indexes"]["pdi_weight_shoving"]),
            Convert.ToDouble(model.Lookups["indexes"]["pdi_weight_potholes"])
        };
        weightsPDI = HelperMethods.NormaliseWeights(weightsPDI);  //Make sure weights add up to 1.0

        //Set up weights for calculating Weighted Sum as PDI.
        // Important: order of weights must match order of distresses. See LAShared.GetSDI() method.
        weightSDI = new double[4] {
            Convert.ToDouble(model.Lookups["indexes"]["sdi_weight_flushing"]),
            Convert.ToDouble(model.Lookups["indexes"]["sdi_weight_scabbing"]),
            Convert.ToDouble(model.Lookups["indexes"]["sdi_weight_mesh_cracks"]),
            Convert.ToDouble(model.Lookups["indexes"]["sdi_weight_potholes"])
        };
        weightSDI = HelperMethods.NormaliseWeights(weightSDI);  //Make sure weights add up to 1.0

        //Set up weights for calculating Weighted Sum as Objective Function Value
        // Important: order of weights must match order of distresses. See LAShared.GetObjective() method.
        weightsObjective = new double[4] {
            Convert.ToDouble(model.Lookups["indexes"]["obj_weight_pdi"]),
            Convert.ToDouble(model.Lookups["indexes"]["obj_weight_sdi"]),
            Convert.ToDouble(model.Lookups["indexes"]["obj_weight_rut"]),
            Convert.ToDouble(model.Lookups["indexes"]["obj_weight_structural"])
        };
        weightsObjective = HelperMethods.NormaliseWeights(weightsObjective);  //Make sure weights add up to 1.0

        WaitTimeBetweenTreatments = model.GetLookupValueNumber("thresholds", "time_between_treatments");
        GapToNextTreatment = model.GetLookupValueNumber("thresholds", "time_to_next_treatment");

        PDI_threshold_entry = model.GetLookupValueNumber("thresholds", "pdi_threshold_entry");
        SDI_threshold_entry = model.GetLookupValueNumber("thresholds", "sdi_threshold_entry");
        PDI_threshold_rehab_ac = model.GetLookupValueNumber("thresholds", "pdi_threshold_ac_rehab");
        PDI_threshold_rehab_chip = model.GetLookupValueNumber("thresholds", "pdi_threshold_chip_rehab");

        PDI_threshold_maint = model.GetLookupValueNumber("maint", "pdi_threshold");
        SDI_threshold_maint = model.GetLookupValueNumber("maint", "sdi_threshold");
        //this.RehabLengthThreshold = model.GetLookupValueNumber("thresholds", "rehab_min_length");

        pdiCalcMethod = LAsharedGen2.GetIndexCalcMethodSafe(model.GetLookupValueText("indexes", "pdi_calc_method"), "PDI");
        sdiCalcMethod = LAsharedGen2.GetIndexCalcMethodSafe(model.GetLookupValueText("indexes", "sdi_calc_method"), "SDI");

        AsphaltYMaxThreshold = model.GetLookupValueNumber("thresholds", "ac_threshold_fwd_ymax");
        AsphaltSCIThreshold = model.GetLookupValueNumber("thresholds", "ac_threshold_fwd_sci");

        IncludePavUseInSurfLifeExpKey = Convert.ToBoolean(model.GetLookupValueText("surf_life_exp", "include_pave_use"));

        ScaleObjectiveByLength = Convert.ToBoolean(model.GetLookupValueText("objective", "scale_obj_by_length"));

        SurfLifeExpLookup = new Dictionary<string, double>();
        Dictionary<string, object> tmp2 = model.Lookups["surf_life_exp"];
        foreach (string key in tmp2.Keys)
        {
            if (key != "include_pave_use")
            {
                SurfLifeExpLookup.Add(key, Convert.ToDouble(tmp2[key]));
            }
        }

        RehabMinLength = model.GetLookupValueNumber("thresholds", "rehab_min_length");
        RehabMaxLength = model.GetLookupValueNumber("thresholds", "rehab_max_length");
        MSDRemLifeThresholdRehab = model.GetLookupValueNumber("thresholds", "msd_remainlife_rehab_max");


    }


    private void SetupMLModels()
    {
        PredictionModels = new Dictionary<string, PredictionEngine<RoadModSegment, Prediction_Binary>>();

        PredictionModels.Add("par_pct_flushing", GetPredictionEngine_Binary("flushing_model.zip"));
        PredictionModels.Add("par_pct_scabbing", GetPredictionEngine_Binary("scabbing_model.zip"));
        PredictionModels.Add("par_pct_lt_cracks", GetPredictionEngine_Binary("lt_cracks_model.zip"));
        PredictionModels.Add("par_pct_mesh_cracks", GetPredictionEngine_Binary("mesh_cracks_model.zip"));
        PredictionModels.Add("par_pctshoving", GetPredictionEngine_Binary("shoving_model.zip"));
        PredictionModels.Add("par_pct_potholes", GetPredictionEngine_Binary("potholes_model.zip"));

        RutRiskModel = GetPredictionEngine_Binary("rutting_model.zip");
        NaasraRiskModel = GetPredictionEngine_Binary("naasra_model.zip");
        
    }

    private PredictionEngine<RoadModSegment, Prediction_Binary> GetPredictionEngine_Binary(string modelFileName)
    {
        string modelFilePath = model.ModelSetup.WorkFolder + @"ml_models\" + modelFileName;

        DataViewSchema modelSchema;        // Define trained model schemas

        //Load the All-In-One Model (data transformation and prediction in a single pipeline)
        ITransformer allInOneModel = mlContext.Model.Load(modelFilePath, out modelSchema);

        //Create the prediction engine
        var engine = mlContext.Model.CreatePredictionEngine<RoadModSegment, Prediction_Binary>(allInOneModel);
        return engine;
    }

    private PredictionEngine<RoadModSegment, Prediction_Number> GetPredictionEngine_Regression(string modelFileName)
    {
        string modelFilePath = model.ModelSetup.WorkFolder + @"ml_models\" + modelFileName;

        DataViewSchema modelSchema;        // Define trained model schemas

        //Load the All-In-One Model (data transformation and prediction in a single pipeline)
        ITransformer allInOneModel = mlContext.Model.Load(modelFilePath, out modelSchema);

        //Create the prediction engine
        var engine = mlContext.Model.CreatePredictionEngine<RoadModSegment, Prediction_Number>(allInOneModel);
        return engine;
    }

    #endregion

    #region Interface Implementation

    public override double[] Initialise(string[] rawRow, double[] newValues)
    {

        RoadModSegment segment = RoadModSegment.LoadFromRawData(model, rawRow);

        double yMax = model.GetRawData_Number(rawRow, "fwd_d0_85");
        double sci = model.GetRawData_Number(rawRow, "fwd_sci_85");

        newValues[PIndex("par_ac_ok")] = LAsharedGen2.CanConsiderAsphaltOverlay(yMax, sci, AsphaltYMaxThreshold, AsphaltSCIThreshold);

        newValues[PIndex("par_struc_remlife")] = segment.RemainingfLife;
        newValues[PIndex("par_struc_deficit")] = segment.StructuralDeficit;
        newValues[PIndex("par_surf_mat")] = model.GetTextToValue("par_surf_mat", segment.SurfMaterial);
        newValues[PIndex("par_surf_class")] = model.GetTextToValue("par_surf_class", segment.SurfClass);
        newValues[PIndex("par_surf_func")] = model.GetTextToValue("par_surf_func", segment.SurfFunction);
        newValues[PIndex("par_surf_thick")] = segment.SurfThickness;
        newValues[PIndex("par_surf_layers")] = segment.SurfLayerCount;
        newValues[PIndex("par_surf_age")] = segment.SurfAge;
       
        newValues[PIndex("par_surf_exp_life")] = segment.SurfLifeExpected;
        newValues[PIndex("par_surf_life_ach")] = 100 * segment.SurfAge / segment.SurfLifeExpected;

        newValues[PIndex("par_pav_age")] = segment.PavementAge;
        newValues[PIndex("par_adt")] = segment.ADT;
        newValues[PIndex("par_heavy")] = segment.HeavyVehicles;

        newValues[PIndex("par_naasra")] = segment.Naasra85th;
        newValues[PIndex("par_rut")] = segment.Rut85th;

        newValues[PIndex("par_pct_flushing")] = segment.PctFlushing;
        newValues[PIndex("par_pct_scabbing")] = segment.PctScabbing;
        newValues[PIndex("par_pct_lt_cracks")] = segment.PctLTCracks;
        newValues[PIndex("par_pct_mesh_cracks")] = segment.PctAlligatorCracks;
        newValues[PIndex("par_pct_shoving")] = segment.PctShoving;
        newValues[PIndex("par_pct_potholes")] = segment.PctPotholes;
       
        double rutIndex = rutToIndexConversionModel.GetValue(segment.Rut85th);
        double pdi = LAsharedGen2.GetPDI(model, newValues, weightsPDI, pdiCalcMethod);
        double sdi = LAsharedGen2.GetSDI(model, newValues, weightSDI, sdiCalcMethod);
        newValues[PIndex("par_pdi")] = pdi;
        newValues[PIndex("par_sdi")] = sdi;

        newValues[PIndex("par_maint_per")] = segment.PeriodsSinceMaintenance;
        newValues[PIndex("par_obj")] = GetUpdatedObjective(pdi, sdi, rutIndex, segment);

        return newValues;
    }

    public override double[] InitialiseForCalibration(string[] rawRow, double[] newValues)
    {
        //First get usual initialisation values for all parameters
        newValues = Initialise(rawRow, newValues);

        RoadModSegment segment = RoadModSegment.LoadFromRawData(model, rawRow);
        //Now adjust only those parameters for initial condition

        newValues[PIndex("par_surf_age")] = 0;
        newValues[PIndex("par_pav_age")] = Math.Max(0, segment.PavementAge - segment.SurfAge);
        newValues[PIndex("par_surf_life_ach")] = 0;

        //Discount traffic to year when surface age was zero. Use smaller historical traffic growth and
        //also guard against values becoming ridiculously small
        double historicalGrowthRate = model.GetLookupValueNumber("traffic", "historical_growth_rate_perc");
        double discFactor = Math.Pow(1 + historicalGrowthRate / 100, -segment.SurfAge);
        newValues[PIndex("par_adt")] = Math.Max(50, segment.ADT * discFactor);
        newValues[PIndex("par_heavy")] = Math.Max(1, segment.HeavyVehicles * discFactor);


        foreach (string distress in distressParams)
        {
            if (segment.SurfFunction == "1")
            {
                newValues[PIndex(distress)] = 0;
            }
            else if (segment.SurfClass == "ac")
            {
                newValues[PIndex(distress)] = 0.1;
            }
            else
            {
                newValues[PIndex(distress)] = 0.5;
            }
        }

        newValues[PIndex("par_sci")] = 0;

        double resetRutAssumed = 3.5;
        double naasraMin = model.GetLookupValueNumber("resets", "naasra_reset_min");
        double rutMin = model.GetLookupValueNumber("resets", "rut_reset_min");
        if (segment.SurfFunction == "1")
        {
            newValues[PIndex("par_naasra")] = naasraMin;
            newValues[PIndex("par_rut")] = resetRutAssumed;
        }
        else if (segment.SurfClass == "ac")
        {
            newValues[PIndex("par_naasra")] = Math.Max(naasraMin, segment.Naasra85th * 0.5);
            newValues[PIndex("par_rut")] = Math.Max(rutMin, segment.Rut85th * 0.5);
        }
        else
        {
            newValues[PIndex("par_naasra")] = Math.Max(naasraMin, segment.Naasra85th * 0.8);
            newValues[PIndex("par_rut")] = Math.Max(rutMin, segment.Rut85th * 0.8);
        }

        double pdi = LAsharedGen2.GetPDI(model, newValues, weightsPDI, pdiCalcMethod);
        double sdi = LAsharedGen2.GetSDI(model, newValues, weightSDI, sdiCalcMethod);
        newValues[PIndex("par_pdi")] = pdi;
        newValues[PIndex("par_sdi")] = sdi;

        newValues[PIndex("par_maint_per")] = 999;

        double rutIndex = rutToIndexConversionModel.GetValue(segment.Rut85th);

        newValues[PIndex("par_obj")] = GetUpdatedObjective(pdi, sdi, rutIndex, segment);

        return newValues;
    }

    public override List<TreatmentStrategy> GetStrategies(ModelBase model, int ielem, int iPeriod, string[] rawRow, double[] prevValues)
    {
        List<TreatmentStrategy> strategies = new List<TreatmentStrategy>();
        int prevEpoch = iPeriod - 1;

        RoadModSegment segment = RoadModSegment.LoadFromModelData(model, rawRow, prevValues);
        segment.PDI = Math.Round(model.GetParameterValue("par_pdi", prevValues), 2);
        segment.SDI = Math.Round(model.GetParameterValue("par_sdi", prevValues), 2);

        int periodsToNextTreatment = model.Treatments.PeriodsToNextTreatment(ielem, iPeriod);

        //Only consider treatments if the time/periods to the next treatment is large enough
        if (periodsToNextTreatment > GapToNextTreatment)
        {
            if (segment.IsSecondCoatSituation(model))
            {
                strategies.Add(GetSecondCoatStrategy(ielem, iPeriod, rawRow, prevValues, segment.AreaM2));
            }
            else if (segment.CanConsiderTreatment(PDI_threshold_entry, SDI_threshold_entry, WaitTimeBetweenTreatments))
            {
                if (segment.ConsiderAsphaltTreatment(model))
                {
                    if (segment.CanConsiderRehab(RehabMinLength, RehabMaxLength, PDI_threshold_rehab_ac, MSDRemLifeThresholdRehab))
                    {
                        strategies.Add(GetRehabACStrategy(ielem, iPeriod, rawRow, prevValues, segment.AreaM2, segment));
                    }

                    double canDoAC = prevValues[PIndex("par_ac_ok")];

                    if (canDoAC > 0)
                    {
                        strategies.Add(GetThinACwithRepairStrategy(ielem, iPeriod, rawRow, prevValues, segment.AreaM2, segment));

                        TreatmentStrategy strat2 = new TreatmentStrategy(ielem, rawRow, prevValues, iPeriod);
                        strat2.AddFirstTreatment("ThinAC", segment.AreaM2, $"pdi = {segment.PDI}; sdi = {segment.SDI}", "none");
                        strat2.AddFollowUpTreatment("ThinAC", 8, segment.AreaM2, "none", "none");
                        strategies.Add(strat2);

                        TreatmentStrategy strat3 = new TreatmentStrategy(ielem, rawRow, prevValues, iPeriod);
                        strat3.AddFirstTreatment("ThinAC", segment.AreaM2, $"pdi = {segment.PDI}; sdi = {segment.SDI}", "none");
                        strat3.AddFollowUpTreatment(GetRehabCode("ac", segment), 8, segment.AreaM2, "none", "none");
                        strategies.Add(strat3);
                    }

                }
                else if (segment.SurfClass == "block")
                {
                    TreatmentStrategy blockStrat = new TreatmentStrategy(ielem, rawRow, prevValues, iPeriod);
                    blockStrat.AddFirstTreatment("BlockPave", segment.AreaM2, $"pdi = {segment.PDI}; sdi = {segment.SDI}", "none");
                    strategies.Add(blockStrat);
                }
                else if (segment.SurfClass == "concrete")
                {
                    TreatmentStrategy concStrat = new TreatmentStrategy(ielem, rawRow, prevValues, iPeriod);
                    concStrat.AddFirstTreatment("ConcreteRep", segment.AreaM2, $"pdi = {segment.PDI}; sdi = {segment.SDI}", "none");
                    strategies.Add(concStrat);
                }
                else
                {
                    //This case handles SEALS but also cases where the existing surface is AC but we are reverting to a seal because NextSurfacing is "seal"
                    if (segment.CanConsiderRehab(RehabMinLength, RehabMaxLength, PDI_threshold_rehab_chip, MSDRemLifeThresholdRehab))
                    {
                        TreatmentStrategy strat1 = GetRehabChipSealStrategy(ielem, iPeriod, rawRow, prevValues, segment.AreaM2, segment);
                        strategies.Add(strat1);
                    }

                    TreatmentStrategy stratPreseal = new TreatmentStrategy(ielem, rawRow, prevValues, iPeriod);
                    stratPreseal.AddFirstTreatment("PreSeal_Rep", segment.AreaM2, $"pdi = {segment.PDI}; sdi = {segment.SDI}", "none");
                    stratPreseal.AddFollowUpTreatment("ChipSeal", 1, segment.AreaM2, "Seal after pre-Seal", "none", true);
                    stratPreseal.AddFollowUpTreatment("ChipSeal", 9, segment.AreaM2, "Follow up", "none");
                    strategies.Add(stratPreseal);

                    TreatmentStrategy strat2 = new TreatmentStrategy(ielem, rawRow, prevValues, iPeriod);
                    strat2.AddFirstTreatment("ChipSeal", segment.AreaM2, $"pdi = {segment.PDI}; sdi = {segment.SDI}", "none");
                    strat2.AddFollowUpTreatment("ChipSeal", 8, segment.AreaM2, "Follw up", "none");
                    strategies.Add(strat2);

                    TreatmentStrategy strat3 = new TreatmentStrategy(ielem, rawRow, prevValues, iPeriod);
                    strat3.AddFirstTreatment("ChipSeal", segment.AreaM2, $"pdi = {segment.PDI}; sdi = {segment.SDI}", "none");
                    strat3.AddFollowUpTreatment(GetRehabCode("seal", segment), 8, segment.AreaM2, "Rehab in future", "none");
                    //ToDo: if forcing later follow up, this may cause duplicate treatment error
                    strat3.AddFollowUpTreatment("ChipSeal", 9, segment.AreaM2, "none", "none");
                    strategies.Add(strat3);
                }
            }
        }

        return strategies;
    }

    public override double[] Increment(string[] rawRow, double[] prevValues)
    {
        double[] newValues = new double[model.NParameters];
        Array.Copy(prevValues, newValues, prevValues.Length); // Assume all values stay the same by default

        RoadModSegment segment = RoadModSegment.LoadFromModelData(model, rawRow, newValues);

        //Take care of Estimated Structural Life
        if (segment.RemainingfLife >= 0)
        {
            segment.RemainingfLife = Math.Max(0, segment.RemainingfLife - 1);
            newValues[PIndex("par_struc_remlife")] = segment.RemainingfLife;
        }

        //Take care of parPavAge, parSurfAge, parSurfLifeAch
        newValues = IncrementPavAgeAndSurfaceAgeAndAchievedLife(segment, newValues);

        //Take care of parADT, parHCV
        newValues = UpdateTrafficParams(segment, rawRow, newValues);

        // Take care of Rut and Naasra BEFORE distresses because distresses are dependent on 
        // Rut and Naasra
        newValues = UpdateRutValue(segment, newValues);
        newValues = UpdateNaasraValue(segment, newValues);

        // Take care of: parCracks_gen, parCracks_fat, parShear, parDeform, parPothole, parSurf_def
        foreach (string distress_key in distressParams)
        {
            int index = PIndex(distress_key);
            double currentValue = newValues[index];
            newValues[index] = GetNextDistressValue(distress_key, currentValue, segment);
        }

        //Take care of par_pdi and par_sdi
        double rutIndex = rutToIndexConversionModel.GetValue(segment.Rut85th);
        double pdi = LAsharedGen2.GetPDI(model, newValues, weightsPDI, pdiCalcMethod);
        double sdi = LAsharedGen2.GetSDI(model, newValues, weightSDI, sdiCalcMethod);
        newValues[PIndex("par_pdi")] = pdi;
        newValues[PIndex("par_sdi")] = sdi;

        //Update the segment with new values
        segment = RoadModSegment.LoadFromModelData(model, rawRow, newValues);
        newValues[PIndex("par_sci")] = SurfCondIndexModel.Predict(segment).Score;

        int maintIndex = PIndex("par_maint_per");
        double periodsSinceMaint = newValues[maintIndex];
        newValues[maintIndex] = periodsSinceMaint + 1;

        newValues[PIndex("par_obj")] = GetUpdatedObjective(pdi, sdi, rutIndex, segment);

        return newValues;

    }

    public override double[] Reset(TreatmentInstance treatment, string[] rawRow, double[] prevValues)
    {

        double[] newValues = new double[model.NParameters];
        Array.Copy(prevValues, newValues, prevValues.Length); // Assume all values stay the same by default

        RoadModSegment segment = RoadModSegment.LoadFromModelData(model, rawRow, newValues);

        UpdateTrafficParams(segment, rawRow, newValues);

        if (treatment.TreatmentName.StartsWith("RehabAC"))
        {
            newValues = GetReset_Rehab(rawRow, newValues, "ac", "AC", 40);
            newValues[PIndex("par_ac_ok")] = 1;  //Can do AC overlay now
        }
        else if (treatment.TreatmentName.StartsWith("RehabCS"))
        {
            newValues = GetReset_Rehab(rawRow, newValues, "seal", "2CHIP", 15);
        }
        else
        {
            switch (treatment.TreatmentName)
            {
                case "ChipSeal":
                    newValues = GetReset_Seal(rawRow, newValues);
                    break;
                case "ThinAC":
                    newValues = GetReset_AC(rawRow, newValues);
                    break;
                case "Project_ChipSeal":
                    newValues = GetReset_Seal(rawRow, newValues);
                    break;
                case "Project_ThinAC":
                    newValues = GetReset_AC(rawRow, newValues);
                    break;
                case "Project_Rehab_AC":
                    newValues = GetReset_Rehab(rawRow, newValues, "ac", "AC", 40);
                    newValues[PIndex("par_ac_ok")] = 1;  //Can do AC overlay now
                    break;
                case "ThinAC_SR":
                    newValues = GetReset_Rehab(rawRow, newValues, "ac", "AC", 40);
                    newValues[PIndex("par_ac_ok")] = 1;  //Can do AC overlay now
                    break;
                case "Project_Rehab_CS":
                    newValues = GetReset_Rehab(rawRow, newValues, "seal", "2CHIP", 15);
                    break;
                case "PreSeal_Rep":
                    newValues = GetReset_PreSealRepair(segment, newValues);
                    break;
                case "BlockPave":
                    newValues = GetReset_Rehab(rawRow, newValues, "block", "INBLK", 150);
                    break;
                case "ConcreteRep":
                    newValues = GetReset_Rehab(rawRow, newValues, "concrete", "CONC", 150);
                    break;
                case "MaintPA":
                    //Only increment surface age and pavement age
                    // For now we assume maintenance does not reset any distresses, but it does 'arrest' it for this year
                    IncrementPavAgeAndSurfaceAgeAndAchievedLife(segment, newValues);
                    newValues[PIndex("par_maint_per")] = 0;
                    break;
                case "MaintSU":
                    // For now we assume maintenance does not reset any distresses, but it does 'arrest' it for this year
                    IncrementPavAgeAndSurfaceAgeAndAchievedLife(segment, newValues);
                    newValues[PIndex("par_maint_per")] = 0;
                    break;
                default:
                    //should not get here unless treatment is some form of rehab
                    if (!treatment.TreatmentName.StartsWith("Rehab"))
                    {
                        throw new Exception($"Treatent type '{treatment.TreatmentName}' is not handled in Resetter");
                    }
                    break;
            }
        }


        //Calculate and update Expected Surface life, but only if it is not maintenance
        if (treatment.TreatmentName != "MaintPA" && treatment.TreatmentName != "MaintSU")
        {
            //Run next lines only after the variables required by the Surf Life model are set!
            segment = RoadModSegment.LoadFromModelData(model, rawRow, newValues);
            double surfLifeExp = surfLifeExp = SurfLifeExpLookup[GetKeyForSurfLifeExpectedLookup(segment)];
            newValues[PIndex("par_surf_exp_life")] = surfLifeExp;
            newValues[PIndex("par_surf_life_ach")] = 0;
        }

        segment = RoadModSegment.LoadFromModelData(model, rawRow, newValues);
        newValues[PIndex("par_sci")] = SurfCondIndexModel.Predict(segment).Score;

        double rutIndex = rutToIndexConversionModel.GetValue(segment.Rut85th);
        double pdi = LAsharedGen2.GetPDI(model, newValues, weightsPDI, pdiCalcMethod);
        double sdi = LAsharedGen2.GetSDI(model, newValues, weightSDI, sdiCalcMethod);
        newValues[PIndex("par_pdi")] = pdi;
        newValues[PIndex("par_sdi")] = sdi;

        newValues[PIndex("par_obj")] = GetUpdatedObjective(pdi, sdi, rutIndex, segment);

        //TODO: Once reports are all done and locked in: The next should only apply if the treatment is not maintenance!
        //Update the segment with new values        
        int maintIndex = PIndex("par_maint_per");
        double periodsSinceMaint = newValues[maintIndex];
        newValues[maintIndex] = periodsSinceMaint + 1;

        return newValues;

    }

    public override TreatmentInstance GetTriggeredMaintenance(ModelBase model, int iElem, int iPeriod, double[] paramValues, string[] rawData)
    {
        int maintIndex = PIndex("par_maint_per");
        double periodsSinceMaint = paramValues[maintIndex];
        double maintFreq = model.GetLookupValueNumber("maint", "maint_frequency");

        double rander = Rando.NextDouble();
        if (rander < 0.5)
        {
            if (periodsSinceMaint >= maintFreq)
            {
                RoadModSegment segment = RoadModSegment.LoadFromModelData(model, rawData, paramValues);
                segment.PDI = Math.Round(model.GetParameterValue("par_pdi", paramValues), 2);
                segment.SDI = Math.Round(model.GetParameterValue("par_sdi", paramValues), 2);
                if (segment.PDI > PDI_threshold_maint || segment.SDI > SDI_threshold_maint)
                {
                    string maintType = GetMaintenanceTreatmentName(segment);
                    if (maintType == "none")
                    {
                        return null;
                    }
                    else
                    {
                        double areaM2 = model.GetRawData_Number(rawData, "area_m2");
                        TreatmentInstance treatment = new TreatmentInstance(iElem, maintType, iPeriod, areaM2, false, "Routine maintenance", $"pdi = {segment.PDI}; sdi = {segment.SDI}");
                        treatment.RankParamSimple = segment.PDI;
                        return treatment;
                    }
                }
            }
        }
        return null;
    }

    #endregion

    #region Trigger Helpers

    private string GetMaintenanceTreatmentName(RoadModSegment segment)
    {
        Prediction_Binary prediction = MaintRiskModelPA.Predict(segment);
        double threshold = Convert.ToDouble(model.Lookups["maint"]["pa_threshold"]);
        if (prediction.Probability > threshold)
        {
            return "MaintPA";
        }
        else
        {
            prediction = MaintRiskModelSU.Predict(segment);
            threshold = Convert.ToDouble(model.Lookups["maint"]["su_threshold"]);
            if (prediction.Probability > threshold)
            {
                return "MaintSU";
            }
        }
        return "none";
    }


    private TreatmentStrategy GetSecondCoatStrategy(int ielem, int iPeriod, string[] rawRow, double[] prevValues, double areaM2)
    {
        TreatmentStrategy strat = new TreatmentStrategy(ielem, rawRow, prevValues, iPeriod);
        strat.AddFirstTreatment("ChipSeal", areaM2, "Second Coat", "Policy", true);
        return strat;
    }

    private TreatmentStrategy GetRehabACStrategy(int ielem, int iPeriod, string[] rawRow, double[] prevValues, double areaM2, RoadModSegment segment)
    {
        try
        {
            string treatCode = GetRehabCode("ac", segment);
            TreatmentStrategy strat = new TreatmentStrategy(ielem, rawRow, prevValues, iPeriod);
            strat.AddFirstTreatment(treatCode, areaM2, $"pdi = {segment.PDI}; sdi = {segment.SDI}", "none");
            strat.AddFollowUpTreatment("ThinAC", 10, areaM2, "", "");
            return strat;
        }
        catch (Exception e)
        {
            throw new Exception($"Error in GetRehabACStrategy. Details: {e.Message}");
        }
    }

    private TreatmentStrategy GetRehabChipSealStrategy(int ielem, int iPeriod, string[] rawRow, double[] prevValues, double areaM2, RoadModSegment segment)
    {
        try
        {
            string treatCode = GetRehabCode("seal", segment);
            TreatmentStrategy strat = new TreatmentStrategy(ielem, rawRow, prevValues, iPeriod);
            strat.AddFirstTreatment(treatCode, segment.AreaM2, $"pdi = {segment.PDI}; sdi = {segment.SDI}", "none");
            int secondCoatWait = Convert.ToInt32(model.GetLookupValueNumber("second coat", segment.ONRC));
            strat.AddFollowUpTreatment("ChipSeal", secondCoatWait, segment.AreaM2, "Second Coat", "Second coat as per policy", true);  //Forced
            strat.AddFollowUpTreatment("ChipSeal", 10, segment.AreaM2, "none", "none");
            return strat;
        }
        catch (Exception e)
        {
            throw new Exception($"Error in GetRehabChipSealStrategy. Details: {e.Message}");
        }
    }

    private string GetRehabCode(string surfType, RoadModSegment segment)
    {
        try
        {
            string typeCode = "";
            if (surfType == "ac")
            {
                typeCode = "AC";
            }
            else if (surfType == "seal")
            {
                typeCode = "CS";
            }
            else
            {
                throw new Exception($"Surface type code '{surfType}' is not handled.");
            }

            string urbanRuralCode = segment.UrbanRural;

            string roadVolumeCode = "";
            if (model.Lists["low_vol_roads"].Contains(segment.ONRC))
            {
                roadVolumeCode = "L";
            }
            else if (model.Lists["med_vol_roads"].Contains(segment.ONRC))
            {
                roadVolumeCode = "M";
            }
            else if (model.Lists["high_vol_roads"].Contains(segment.ONRC))
            {
                roadVolumeCode = "H";
            }
            else
            {
                throw new Exception($"ONRC code '{segment.ONRC}' is not handled.");
            }

            return "Rehab" + typeCode + "_" + urbanRuralCode + roadVolumeCode;

        }
        catch (Exception e)
        {
            throw new Exception($"Error in GetRehabPostFixCode. Details: {e.Message}");
        }
    }


    private TreatmentStrategy GetThinACwithRepairStrategy(int ielem, int iPeriod, string[] rawRow, double[] prevValues, double areaM2, RoadModSegment segment)
    {
        TreatmentStrategy strat = new TreatmentStrategy(ielem, rawRow, prevValues, iPeriod);
        strat.AddFirstTreatment("ThinAC_SR", areaM2, $"pdi = {segment.PDI}; sdi = {segment.SDI}", "none");
        strat.AddFollowUpTreatment("ThinAC", 8, areaM2, "", "");
        return strat;
    }

    #endregion

    #region Reset Helpers

    private double[] GetReset_Rehab(string[] rawRow, double[] newValues, string surfType, string surfMat, double surfthick)
    {

        newValues[PIndex("par_surf_mat")] = model.GetTextToValue("par_surf_mat", surfMat);
        newValues[PIndex("par_surf_class")] = model.GetTextToValue("par_surf_class", surfType);
        newValues[PIndex("par_pav_age")] = 0;

        if (surfType == "ac" || surfType == "block" || surfType == "concrete")
        {
            // For AC rehab, update Surface Function to "R" and not "1"
            newValues[PIndex("par_surf_func")] = model.GetTextToValue("par_surf_func", "2");
        }
        else
        {
            // For Seals, update Surface Function to "1"
            newValues[PIndex("par_surf_func")] = model.GetTextToValue("par_surf_func", "1");
        }

        newValues[PIndex("par_surf_age")] = 0;

        newValues[PIndex("par_surf_thick")] = surfthick;  //ToDo: Surface thickness after AC/Chip Rehab?

        //Run next lines only after the variables required by the Surf Life model are set!
        RoadModSegment segment = RoadModSegment.LoadFromModelData(model, rawRow, newValues);

        double surfLifeExp = SurfLifeExpLookup[GetKeyForSurfLifeExpectedLookup(segment)];
        newValues[PIndex("par_surf_exp_life")] = surfLifeExp;
        newValues[PIndex("par_surf_life_ach")] = 0;

        newValues[PIndex("par_naasra")] = NaasraMin;
        newValues[PIndex("par_rut")] = RutMin;

        newValues[PIndex("par_cracks_gen")] = 0;
        newValues[PIndex("par_cracks_struc")] = 0;
        newValues[PIndex("par_struc_fail")] = 0;
        newValues[PIndex("par_deform")] = 0;
        newValues[PIndex("par_pothole")] = 0;
        newValues[PIndex("par_surf_defects")] = 0;

        newValues[PIndex("par_struc_deficit")] = 0.5;
        newValues[PIndex("par_struc_remlife")] = ResetRemLifeAfterRehab;

        return newValues;

    }

    private double[] GetReset_Seal(string[] rawRow, double[] newValues)
    {
        string surfFunc = model.GetParamValueToText("par_surf_func", newValues);
        if (surfFunc == "1")
        {
            newValues[PIndex("par_surf_func")] = model.GetTextToValue("par_surf_func", "2");
        }
        else
        {
            newValues[PIndex("par_surf_func")] = model.GetTextToValue("par_surf_func", "R");
        }


        newValues[PIndex("par_surf_age")] = 0;

        double naasraPercPost = Convert.ToDouble(model.Lookups["resets"]["seal_naasra_perc_post"]);
        double currentNaasra = newValues[PIndex("par_naasra")];
        newValues[PIndex("par_naasra")] = Math.Clamp(currentNaasra * naasraPercPost, NaasraMin, NaasraMax);

        double rutPercPost = Convert.ToDouble(model.Lookups["resets"]["seal_rut_perc_post"]);
        double currentRut = newValues[PIndex("par_rut")];
        newValues[PIndex("par_rut")] = Math.Clamp(currentRut * rutPercPost, RutMin, RutMax);

        double surfLayers = newValues[PIndex("par_surf_layers")];
        newValues[PIndex("par_surf_layers")] = surfLayers + 1;

        double pavAge = newValues[PIndex("par_pav_age")];
        newValues[PIndex("par_pav_age")] = pavAge + 1;

        double surfThick = newValues[PIndex("par_surf_thick")];
        newValues[PIndex("par_surf_thick")] = surfThick + 15;

        foreach (string distressParam in distressParams)
        {
            string key = "seal_" + distressParam + "_reset";
            double distressReset = Convert.ToDouble(model.Lookups["resets"][key]);
            newValues[PIndex(distressParam)] = GetUpdatedDistressValueAfterSealOrPreSeal(distressParam, newValues, distressReset);
        }

        //For Seal, no reset for Structural Deficit

        return newValues;

    }

    private double[] GetReset_PreSealRepair(RoadModSegment segment, double[] newValues)
    {
        double naasraPercPost = Convert.ToDouble(model.Lookups["resets"]["preseal_naasra_perc_post"]);
        double currentNaasra = newValues[PIndex("par_naasra")];
        newValues[PIndex("par_naasra")] = Math.Clamp(currentNaasra * naasraPercPost, NaasraMin, NaasraMax);

        double rutPercPost = Convert.ToDouble(model.Lookups["resets"]["preseal_rep_rut_perc_post"]);
        double currentRut = newValues[PIndex("par_rut")];
        newValues[PIndex("par_rut")] = Math.Clamp(currentRut * rutPercPost, RutMin, RutMax);

        IncrementPavAgeAndSurfaceAgeAndAchievedLife(segment, newValues);

        foreach (string distressParam in distressParams)
        {
            string key = "preseal_" + distressParam + "_reset";
            double distressReset = Convert.ToDouble(model.Lookups["resets"][key]);
            newValues[PIndex(distressParam)] = GetUpdatedDistressValueAfterSealOrPreSeal(distressParam, newValues, distressReset);
        }

        double strucDeficit = newValues[PIndex("par_struc_deficit")];
        newValues[PIndex("par_struc_deficit")] = Math.Max(0, strucDeficit - 1);

        return newValues;

    }

    private double[] GetReset_ThinACwithStrucRepair(RoadModSegment segment, double[] newValues)
    {
        double naasraPercPost = Convert.ToDouble(model.Lookups["resets"]["ac_sr_naasra_perc_post"]);
        double currentNaasra = newValues[PIndex("par_naasra")];
        newValues[PIndex("par_naasra")] = Math.Clamp(currentNaasra * naasraPercPost, NaasraMin, NaasraMax);

        double rutPercPost = Convert.ToDouble(model.Lookups["resets"]["ac_sr_rut_perc_post"]);
        double currentRut = newValues[PIndex("par_rut")];
        newValues[PIndex("par_rut")] = Math.Clamp(currentRut * rutPercPost, RutMin, RutMax);

        IncrementPavAgeAndSurfaceAgeAndAchievedLife(segment, newValues);

        foreach (string distressParam in distressParams)
        {
            string key = "ac_sr_" + distressParam + "_reset";
            double distressReset = Convert.ToDouble(model.Lookups["resets"][key]);
            newValues[PIndex(distressParam)] = GetUpdatedDistressValueAfterSealOrPreSeal(distressParam, newValues, distressReset);
        }

        double strucDeficit = newValues[PIndex("par_struc_deficit")];
        newValues[PIndex("par_struc_deficit")] = Math.Max(0, strucDeficit - 1);

        return newValues;

    }

    private double[] GetReset_AC(string[] rawRow, double[] newValues)
    {

        string surfFunc = model.GetParamValueToText("par_surf_func", newValues);
        if (surfFunc == "1")
        {
            newValues[PIndex("par_surf_func")] = model.GetTextToValue("par_surf_func", "2");
        }
        else
        {
            newValues[PIndex("par_surf_func")] = model.GetTextToValue("par_surf_func", "R");
        }

        newValues[PIndex("par_surf_age")] = 0;

        double naasraPercPost = model.GetLookupValueNumber("resets", "ac_naasra_perc_post");
        double currentNaasra = newValues[PIndex("par_naasra")];
        newValues[PIndex("par_naasra")] = Math.Clamp(currentNaasra * naasraPercPost, NaasraMin, NaasraMax);

        double rutPercPost = model.GetLookupValueNumber("resets", "ac_rut_perc_post");
        double currentRut = newValues[PIndex("par_rut")];
        newValues[PIndex("par_rut")] = Math.Clamp(currentRut * rutPercPost, RutMin, RutMax);

        double pavAge = newValues[PIndex("par_pav_age")];
        newValues[PIndex("par_pav_age")] = pavAge + 1;

        double surfLayers = newValues[PIndex("par_surf_layers")];
        newValues[PIndex("par_surf_layers")] = surfLayers + 1;

        double surfThick = newValues[PIndex("par_surf_thick")];
        newValues[PIndex("par_surf_thick")] = surfThick + 40;

        double strucDeficit = newValues[PIndex("par_struc_deficit")];
        newValues[PIndex("par_struc_deficit")] = Math.Max(0, strucDeficit - 0.5);

        foreach (string distressParam in distressParams)
        {
            string key = "ac_" + distressParam + "_reset";
            double distressReset = Convert.ToDouble(model.Lookups["resets"][key]);
            newValues[PIndex(distressParam)] = GetUpdatedDistressValueAfterSealOrPreSeal(distressParam, newValues, distressReset);
        }

        //par_pdi    - calculated after return in generic caller
        //par_sdi    - calculated after return in generic caller
        //par_obj    - ditto

        return newValues;

    }

    private double GetUpdatedDistressValueAfterSealOrPreSeal(string paramName, double[] newValues, double resetValue)
    {
        double currentValue = newValues[PIndex(paramName)];
        if (currentValue == 0) return 0;
        return Math.Min(resetValue, currentValue);
    }

    #endregion

    #region General Helpers

    private double GetUpdatedObjective(double pdi, double sdi, double rutIndex, RoadModSegment segment)
    {
        double objective = LAsharedGen2.GetObjective_COST354(pdi, sdi, rutIndex, segment.StructuralDeficit, weightsObjective);
        if (ScaleObjectiveByLength) { objective = objective * segment.Length; }
        return objective;
    }

    private string GetKeyForSurfLifeExpectedLookup(RoadModSegment segment)
    {
        switch (segment.SurfClass)
        {
            case "concrete":
                return "concrete_undefined";

            case "block":
                return "block_undefined";
            default:

                string key = "unknown";
                if (IncludePavUseInSurfLifeExpKey)
                {
                    key = segment.UrbanRural + segment.PavementUse + "_" + segment.SurfFunction + "_" + segment.SurfMaterial;
                }
                else
                {
                    key = segment.UrbanRural + "_" + segment.SurfFunction + "_" + segment.SurfMaterial;
                }

                if (SurfLifeExpLookup.ContainsKey(key) == false)
                {
                    model.Warnings.Add($"Surface life lookup key '{key}' is not handled. Assigning default Expected Life");
                    //First try to get a key based on material Class ('ac' or 'sea') using 'undefined' default:
                    key = segment.SurfClass + "_undefined";
                    if (SurfLifeExpLookup.ContainsKey(key) == false)
                    {
                        //If there is also no key to catch-undefined, then throw an error
                        throw new Exception($"The surface life key '{key}' is not handled in your lookups");
                    }
                    else
                    {
                        return key;
                    }
                }
                return key;
        }
    }

    #endregion

    #region Increment Helpers

    private double[] IncrementPavAgeAndSurfaceAgeAndAchievedLife(RoadModSegment segment, double[] newValues)
    {
        segment.PavementAge++;
        segment.SurfAge++;
        segment.SurfLifeAchievedPercent = 100 * segment.SurfAge / segment.SurfLifeExpected;

        newValues[PIndex("par_pav_age")] = segment.PavementAge;
        newValues[PIndex("par_surf_age")] = segment.SurfAge;
        newValues[PIndex("par_surf_life_ach")] = segment.SurfLifeAchievedPercent;
        //Note: SurfLifeExpected does not change here. It only change when a reset is applied

        return newValues;
    }

    private double[] UpdateTrafficParams(RoadModSegment segment, string[] rawRow, double[] newValues)
    {
        //Update ADT with traffic growth               
        segment.ADT = segment.ADT * (1 + TrafficGrowthRate / 100);
        segment.HeavyVehicles = segment.HeavyVehicles * (1 + TrafficGrowthRate / 100);

        newValues[PIndex("par_adt")] = segment.ADT;
        newValues[PIndex("par_heavy")] = segment.HeavyVehicles;

        return newValues;

    }

    private double GetNextDistressValue(string distressKey, double currentValue, RoadModSegment obs)
    {

        //double rand = Rando.NextDouble();
        Prediction_Binary prediction = PredictionModels[distressKey].Predict(obs);

        string key0 = distressKey + "_k0";
        string key1 = distressKey + "_k3";

        //double threshold = 0;
        if (distressKey == "par_cracks_struc")
        {
            int kk = 9;
        }

        if (currentValue == 0)
        {
            //No distress yet. Stochastically initialise or not depending on scaled probability
            double initialValue = 0.001;   // Initial distress state assigned after initialisation
            double scaleFact_Initialise = model.GetLookupValueNumber("increments", key0);
            double taci = scaleFact_Initialise * (1 - prediction.Probability);
            if (obs.SurfAge >= taci)
            {
                return initialValue;  //Distress initialised - return initial distress assigned
            }
            else
            {
                return 0;  //remains zero (no distress)
            }
        }
        else
        {
            // Already some distress. Increase distress state based on probability and 
            // calibrated S-curve growth
            double scaleFact_Growth = model.GetLookupValueNumber("increments", key1);
            PieceWiseLinearModel progressionModel = DistressProgressionModels[distressKey];
            double incremFact = progressionModel.GetValue(currentValue);
            double increment = scaleFact_Growth * incremFact * prediction.Probability;
            return currentValue + increment;
        }
    }

    private double[] UpdateRutValue(RoadModSegment segment, double[] newValues)
    {
        Prediction_Binary prediction = RutRiskModel.Predict(segment);
        double scaleFact = model.GetLookupValueNumber("increments", "rut_increm_k0");

        int index = PIndex("par_rut");
        double rutValue = newValues[index];
        //double rand = Rando.NextDouble();
        //double adjustmentFactor = this.rutIncremAdjustmentModel.GetValue(segment.PavementAge);
        //double adjustmentFactor = prediction.Probability * scaleFact;
        double rutIncrem = scaleFact * rutIncremModel.GetValue(prediction.Probability);
        double newRut = rutValue + rutIncrem;
        newValues[index] = newRut;
        segment.Rut85th = (float)newRut;
        return newValues;
    }

    private double[] UpdateNaasraValue(RoadModSegment segment, double[] newValues)
    {
        Prediction_Binary prediction = NaasraRiskModel.Predict(segment);
        double scaleFact = model.GetLookupValueNumber("increments", "naasra_increm_k0");

        int index = PIndex("par_naasra");
        double naasraValue = newValues[index];
        //double rand = Rando.NextDouble();
        //double adjustmentFactor = prediction.Probability * scaleFact;
        //double increm = adjustmentFactor * this.naasraIncremModel.GetValue(rand);                
        double increm = scaleFact * naasraIncremModel.GetValue(prediction.Probability);
        double newNaasra = naasraValue + increm;
        newValues[index] = newNaasra;
        segment.Naasra85th = (float)newNaasra;
        return newValues;
    }

    #endregion


}
