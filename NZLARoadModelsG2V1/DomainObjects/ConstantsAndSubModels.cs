using DocumentFormat.OpenXml.EMMA;
using JCass_Data.Objects;
using JCass_Data.Utils;
using JCass_ModelCore.ModelObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NZLARoadModelsG2V1.DomainObjects;

internal class ConstantsAndSubModels
{

    #region Variables

    /// <summary>
    /// Model parameter codes that map to distress parameters. Must be in same order as 'DistressRawColumns'
    /// </summary>
    public readonly List<string> DistressParamCodes = new List<string>() { "par_pct_flushing", "par_pct_scabbing", "par_pct_lt_cracks", "par_pct_mesh_cracks", "par_pct_shoving", "par_pct_potholes" };

    /// <summary>
    /// Raw data columns that map to distress percentage values. Must be in same order as 'DistressParams'
    /// </summary>
    public readonly List<string> DistressRawColumns = new List<string>() { "pct_flush", "pct_scabb", "pct_lt_crax", "pct_allig", "pct_shove", "pct_poth" };

    public readonly Dictionary<string, DistressProbabilityModel> DistressProbabilityModels;

    public readonly DistressProbabilityModel RutProbabilityModel;
    public readonly DistressProbabilityModel RoughnessProbabilityModel;

    public readonly double[] WeightsPDI;            //Weights for calculating PDI as weighted sum
    public readonly double[] WeightSDI;             //Weights for calculating SDI as weighted sum
    public readonly double[] WeightsObjective;       //Weights for calculating Objective Function Value as weighted sum

    public readonly float TrafficGrowthRate;

    public readonly double NaasraMin;
    public readonly double NaasraMax;

    public readonly double RutMin;
    public readonly double RutMax;

    public readonly double WaitTimeBetweenTreatments;
    public readonly double GapToNextTreatment;
    public readonly double PDI_threshold_entry;
    public readonly double SDI_threshold_entry;
    public readonly double PDI_threshold_rehab_ac;
    public readonly double PDI_threshold_rehab_chip;

    public readonly double PDI_threshold_maint;
    public readonly double SDI_threshold_maint;

    public readonly double AsphaltYMaxThreshold;
    public readonly double AsphaltSCIThreshold;

    public readonly double RehabMinLength;
    public readonly double RehabMaxLength;

    public readonly double MSDRemLifeThresholdRehab;

    public readonly double ResetRemLifeAfterRehab;

    public readonly bool IncludePavUseInSurfLifeExpKey = true;

    public readonly bool ScaleObjectiveByLength = false;

    public readonly string PdiCalcMethod;
    public readonly string SdiCalcMethod;

    public Dictionary<string, double> SurfLifeExpLookup;

    #endregion

    public ConstantsAndSubModels(ModelBase model)
    {
        TrafficGrowthRate = Convert.ToSingle(model.Lookups["traffic"]["adt_growth_rate_perc"]);

        NaasraMin = Convert.ToDouble(model.Lookups["resets"]["naasra_reset_min"]);
        NaasraMax = Convert.ToDouble(model.Lookups["resets"]["naasra_reset_max"]);

        RutMin = Convert.ToDouble(model.Lookups["resets"]["rut_reset_min"]);
        RutMax = Convert.ToDouble(model.Lookups["resets"]["rut_reset_max"]);

        ResetRemLifeAfterRehab = Convert.ToDouble(model.Lookups["resets"]["rehab_remlife_reset"]);

        //Set up weights for calculating Weighted Sum as PDI.
        // Important: order of weights must match order of distresses. See LAShared.GetPDI() method.
        WeightsPDI = new double[4] {
            Convert.ToDouble(model.Lookups["indexes"]["pdi_weight_lt_cracks"]),
            Convert.ToDouble(model.Lookups["indexes"]["pdi_weight_mesh_cracks"]),
            Convert.ToDouble(model.Lookups["indexes"]["pdi_weight_shoving"]),
            Convert.ToDouble(model.Lookups["indexes"]["pdi_weight_potholes"])
        };
        WeightsPDI = JCass_Core.Utils.HelperMethods.NormaliseWeights(WeightsPDI);  //Make sure weights add up to 1.0

        //Set up weights for calculating Weighted Sum as PDI.
        // Important: order of weights must match order of distresses. See LAShared.GetSDI() method.
        WeightSDI = new double[4] {
            Convert.ToDouble(model.Lookups["indexes"]["sdi_weight_flushing"]),
            Convert.ToDouble(model.Lookups["indexes"]["sdi_weight_scabbing"]),
            Convert.ToDouble(model.Lookups["indexes"]["sdi_weight_mesh_cracks"]),
            Convert.ToDouble(model.Lookups["indexes"]["sdi_weight_potholes"])
        };
        WeightSDI = JCass_Core.Utils.HelperMethods.NormaliseWeights(WeightSDI);  //Make sure weights add up to 1.0

        //Set up weights for calculating Weighted Sum as Objective Function Value
        // Important: order of weights must match order of distresses. See LAShared.GetObjective() method.
        WeightsObjective = new double[3] {
            Convert.ToDouble(model.Lookups["indexes"]["obj_weight_pdi"]),
            Convert.ToDouble(model.Lookups["indexes"]["obj_weight_sdi"]),
            //Convert.ToDouble(model.Lookups["indexes"]["obj_weight_rut"]),
            Convert.ToDouble(model.Lookups["indexes"]["obj_weight_structural"])
        };
        WeightsObjective = JCass_Core.Utils.HelperMethods.NormaliseWeights(WeightsObjective);  //Make sure weights add up to 1.0

        WaitTimeBetweenTreatments = model.GetLookupValueNumber("thresholds", "time_between_treatments");
        GapToNextTreatment = model.GetLookupValueNumber("thresholds", "time_to_next_treatment");

        PDI_threshold_entry = model.GetLookupValueNumber("thresholds", "pdi_threshold_entry");
        SDI_threshold_entry = model.GetLookupValueNumber("thresholds", "sdi_threshold_entry");
        PDI_threshold_rehab_ac = model.GetLookupValueNumber("thresholds", "pdi_threshold_ac_rehab");
        PDI_threshold_rehab_chip = model.GetLookupValueNumber("thresholds", "pdi_threshold_chip_rehab");

        PDI_threshold_maint = model.GetLookupValueNumber("maint", "pdi_threshold");
        SDI_threshold_maint = model.GetLookupValueNumber("maint", "sdi_threshold");
        //this.RehabLengthThreshold = model.GetLookupValueNumber("thresholds", "rehab_min_length");

        PdiCalcMethod = LAsharedGen2.GetIndexCalcMethodSafe(model.GetLookupValueText("indexes", "pdi_calc_method"), "PDI");
        SdiCalcMethod = LAsharedGen2.GetIndexCalcMethodSafe(model.GetLookupValueText("indexes", "sdi_calc_method"), "SDI");

        AsphaltYMaxThreshold = model.GetLookupValueNumber("thresholds", "ac_threshold_fwd_ymax");
        AsphaltSCIThreshold = model.GetLookupValueNumber("thresholds", "ac_threshold_fwd_sci");

        IncludePavUseInSurfLifeExpKey = Convert.ToBoolean(model.GetLookupValueText("surf_life_exp", "include_pave_use"));

        ScaleObjectiveByLength = Convert.ToBoolean(model.GetLookupValueText("objective", "scale_obj_by_length"));

        RehabMinLength = model.GetLookupValueNumber("thresholds", "rehab_min_length");
        RehabMaxLength = model.GetLookupValueNumber("thresholds", "rehab_max_length");
        MSDRemLifeThresholdRehab = model.GetLookupValueNumber("thresholds", "msd_remainlife_rehab_max");


        string coefficientsFilePath = model.Setup.WorkFolder + @"ml_models\logistic_regression_coeffs.csv";
        this.DistressProbabilityModels = new Dictionary<string, DistressProbabilityModel>();
        jcDataSet modelCoeffs = CSVHelper.ReadDataFromCsvFile(coefficientsFilePath, "distress");
        for (int i = 0; i < this.DistressParamCodes.Count; i++)
        {
            string paramCode = this.DistressParamCodes[i];
            string rawColCode = this.DistressRawColumns[i];
            this.DistressProbabilityModels.Add(paramCode, new DistressProbabilityModel(modelCoeffs.Row(rawColCode)));
        }

        coefficientsFilePath = model.Setup.WorkFolder + @"ml_models\logistic_regression_rut_rough_coeffs.csv";
        modelCoeffs = CSVHelper.ReadDataFromCsvFile(coefficientsFilePath, "distress");
        this.RutProbabilityModel = new DistressProbabilityModel(modelCoeffs.Row("rutting"));
        this.RoughnessProbabilityModel = new DistressProbabilityModel(modelCoeffs.Row("naasra_85"));
        
        SurfLifeExpLookup = new Dictionary<string, double>();
        Dictionary<string, object> tmp2 = model.Lookups["surf_life_exp"];
        foreach (string key in tmp2.Keys)
        {
            if (key != "include_pave_use")
            {
                SurfLifeExpLookup.Add(key, Convert.ToDouble(tmp2[key]));
            }
        }

    }

    
}
