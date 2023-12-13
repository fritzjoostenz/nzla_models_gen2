using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.EMMA;
using JCass_Core.Utils;
using JCass_Data.Objects;
using JCass_Data.Utils;
using JCass_ModelCore.Customiser;
using JCass_ModelCore.ModelObjects;
using JCass_ModelCore.Treatments;
using Microsoft.ML;
using MLModelClasses.DomainClasses;
using MLModelClasses.MLClasses;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NZLARoadModelsG2V1.DomainObjects;

public class RoadModSegmentV1_Dev: JCass_ModelCore.Customiser.ModelElementBase
{

    #region Variables

    private readonly ConstantsAndSubModels SetupInfo;
       
    private double _pdi;
    private double _sdi;
    private double _objective;

    private DistressProgressionModel FlushingProgressionModel;
    private DistressProgressionModel ScabbingProgressionModel;
    private DistressProgressionModel LTcracksProgressionModel;
    private DistressProgressionModel MeshCracksProgressionModel;
    private DistressProgressionModel ShovingProgressionModel;
    private DistressProgressionModel PotholeProgressionModel;

    #endregion

    #region Properties - Data/Parameter Bound 

    
    
           
    
    #endregion

    #region Properties - Traffic

   
    #endregion

    #region Properties - Surfacing

    [ModelElement("none", "par_surf_age")]
    public double SurfAge { get; set; }

    [ModelElement("surf_date", "none")]
    public string SurfDate { get; set; }

    [ModelElement("surf_class", "par_surf_class")]
    public string SurfClass { get; set; }

    [ModelElement("surf_function", "par_surf_func")]
    public String SurfFunction { get; set; }

    [ModelElement("surf_layer_no", "par_surf_layers")]
    public double SurfLayerCount { get; set; }

    [ModelElement("surf_thick", "par_surf_thick")]
    public double SurfThickness { get; set; }

    [ModelElement("surf_width", "none")]
    public float SurfWidth { get; set; }

    [ModelElement("surf_material", "par_surf_mat")]
    public string SurfMaterial { get; set; }

    [ModelElement("surf_life_expected", "par_surf_exp_life")]
    public double SurfLifeExpected { get; set; }

    [ModelElement("none", "par_surf_life_ach")]
    public double SurfLifeAchievedPercent
    {
        get { return (float)(100 * this.SurfAge / this.SurfLifeExpected); }
    }

    [ModelElement("none", "par_surf_remain_life")]
    public double SurfLifeRemaining
    {
        get { return this.SurfLifeExpected - this.SurfAge; }
    }

    [ModelElement("none", "par_ac_ok")]
    public double ACisOK { get; set; }

    /// <summary>
    /// Code for the next surfacing - this should be "ac" or "seal". It can be used to check if any future surfacings
    /// should change type, e.g. if the policy is to downgrade "ac" to "seal" under some situations. This flag should
    /// be determined in the pre-processing based on the client policy
    /// </summary>
    [ModelElement("next_surf", "none")]
    public string NextSurfacing { get; set; }

    #endregion

    #region Properties - Pavement/Structural Capacity

    [ModelElement("none", "par_pav_age")]
    public double PavementAge { get; set; }

    [ModelElement("pave_date", "none")]
    public string PavementDate { get; set; }


    [ModelElement("msd_remlife_lwp", "none")]
    public float MSDRemainingLifeLWP { get; set; }

    [ModelElement("msd_remlife_rwp", "none")]
    public float MSDRemainingLifeRWP { get; set; }

    [ModelElement("none", "par_struc_remlife")]
    public double RemainingStructuralLife { get; set; }


    [ModelElement("fwd_d0_85", "none")]
    public float FwdD085th { get; set; }

    [ModelElement("fwd_sci_85", "none")]
    public float FwdSCI85th { get; set; }

    public double CanConsiderAsphaltOverlay
    {
        get
        {            
            double yMaxThreshold = this.SetupInfo.AsphaltYMaxThreshold;
            double sciThreshold = this.SetupInfo.AsphaltSCIThreshold;
            if (this.FwdD085th > yMaxThreshold) { return 0; }
            if (this.FwdSCI85th > sciThreshold) { return 0; }
            return 1;
        }        
    }

    #endregion

    #region Properties - Distresses/Condition 

    [ModelElement("hsd_date", "none")]
    public string HSDSurveyDate { get; set; }

    
    public double HSDSurveyAge { get; set; }
    
    [ModelElement("rut_lwpmean_85", "none")]
    public double RutLWP85th { get; set; }

    [ModelElement("rut_rwpmean_85", "none")]
    public double RutRWP85th { get; set; }

    [ModelElement("none", "par_rut")]
    public double Rut85th { get; set; }

    [ModelElement("none", "par_naasra")]
    public double Naasra85th { get; set; }
    
    [ModelElement("none", "par_pdi")]
    public double PDI
    {
        get
        {
            return this._pdi;
        }
    }

    [ModelElement("none", "par_sdi")]
    public double SDI
    {
        get
        {
            return this._sdi;
        }
    }

    [ModelElement("none", "par_obj")]
    public double Objective
    {
        get
        {
            return this._objective;
        }
    }

    [ModelElement("cond_survey_date", "none")]
    public string VisCondSurveyDate { get; set; }

    public double VisCondSurveyAge { get; set; }

    [ModelElement("pct_flush", "par_pct_flushing")]
    public double PctFlushing { get; set; }

    [ModelElement("pct_scabb", "par_pct_scabbing")]
    public double PctScabbing { get; set; }

    [ModelElement("pct_lt_crax", "par_pct_lt_cracks")]
    public double PctLTcracks { get; set; }

    [ModelElement("pct_allig", "par_pct_mesh_cracks")]
    public double PctMeshCracks { get; set; }

    [ModelElement("pct_shove", "par_pct_shoving")]
    public double PctShoving { get; set; }

    [ModelElement("pct_poth", "par_pct_potholes")]
    public double PctPotholes { get; set; }

    #endregion

    #region Constructor and Getters

    internal RoadModSegmentV1_Dev(int elemIndex, ModelBase modelBase, ConstantsAndSubModels constants):base(elemIndex, modelBase)
    {
        this.SetupInfo = constants;        
    }
    
    public void InitialiseFromRawData(string[] rawDataRow)
    {
        this.SetupFromRawData(rawDataRow);

        //Handle special properties not represented in raw data that need to be calculated
        this.SurfAge = this.GetYearsElapsedFromDate(this.SurfDate);
        this.PavementAge = this.GetYearsElapsedFromDate(this.PavementDate);
        this.HSDSurveyAge = this.GetYearsElapsedFromDate(this.HSDSurveyDate);
        this.VisCondSurveyAge = this.GetYearsElapsedFromDate(this.VisCondSurveyDate);

        this.RemainingStructuralLife = this.GetInitialRemainingStructuralLife();    
        this.Rut85th = GetInitialRut();
        this.Naasra85th = GetInitialNaasra();

        //this.FlushingProgressionModel = this.GetDistressProgressionModel("par_pct_flushing");
        //this.ScabbingProgressionModel = this.GetDistressProgressionModel("par_pct_scabbing");
        //this.LTcracksProgressionModel = this.GetDistressProgressionModel("par_pct_lt_cracks");
        //this.MeshCracksProgressionModel = this.GetDistressProgressionModel("par_pct_mesh_cracks");
        //this.ShovingProgressionModel = this.GetDistressProgressionModel("par_pct_shoving");
        //this.PotholeProgressionModel = this.GetDistressProgressionModel("par_pct_potholes");

        this.ACisOK = Convert.ToDouble(this.NextSurfacing == "ac"); //Assume like for like initially. ToDO: add custom decision field in raw data
        
        this.UpdateCalculatedProperties();
    }
 
    public void Increment()
    {
        //No changes to these parameters for Incrementing (without treatment):
        //par_is_rehab_route, par_ac_ok, par_struc_deficit,
        //par_surf_exp_life, par_surf_mat, par_surf_class, par_surf_func, par_surf_thick, par_surf_layers
        
        //Take care of Estimated Structural Life. Maps to par_struc_remlife
        if (this.RemainingStructuralLife >= 0) {this.RemainingStructuralLife = Math.Max(0, this.RemainingStructuralLife - 1); }

        //Take care of parPavAge, parSurfAge
        //NOTE: updating surfAge will automatically update properties mappint to par_surf_remain_life and parSurfLifeAch
        this.PavementAge++;
        this.SurfAge++;
        //Note: SurfLifeExpected does not change here. It only change when a reset is applied

        //Update ADT with traffic growth. Maps to par_adt.
        //NOTE: updating ADT will automatically update property mapping to par_heavy
        this.ADT = this.ADT * (1 + this.SetupInfo.TrafficGrowthRate / 100);

        // Handle distresses that increment with S-progression model
        // Covers: par_pct_flushing, par_pct_scabbing, par_pct_lt_cracks, par_pct_mesh_cracks, par_pct_shoving, par_pct_potholes
        this.PctFlushing += this.FlushingProgressionModel.GetIncrement(this.SurfAge);
        this.PctScabbing += this.ScabbingProgressionModel.GetIncrement(this.SurfAge);
        this.PctLTcracks += this.LTcracksProgressionModel.GetIncrement(this.SurfAge);
        this.PctMeshCracks += this.MeshCracksProgressionModel.GetIncrement(this.SurfAge);
        this.PctShoving += this.ShovingProgressionModel.GetIncrement(this.SurfAge);
        this.PctPotholes += this.PotholeProgressionModel.GetIncrement(this.SurfAge);


        //ToDo: Rut and Naasra Increments mapping to par_rut and par_naasra                  
        //newValues = UpdateRutValue(segment, newValues);
        //newValues = UpdateNaasraValue(segment, newValues);

        //Take care of par_pdi, par_sdi and par_obj
        this.UpdateCalculatedProperties();
    }

    public void Reset(string treatmentName)
    {
        //No changes to these parameters for Incrementing (without treatment):
        //par_is_rehab_route, par_ac_ok, par_struc_deficit,
        //par_surf_exp_life, par_surf_mat, par_surf_class, par_surf_func, par_surf_thick, par_surf_layers

        //Update ADT with traffic growth. Maps to par_adt.
        //NOTE: updating ADT will automatically update property mapping to par_heavy
        this.ADT = this.ADT * (1 + this.SetupInfo.TrafficGrowthRate / 100);

        if (treatmentName.StartsWith("RehabAC"))
        {
            this.ResetRehab(40);
            this.ACisOK = 1;  //Can do AC overlay now
        }
        else if (treatmentName.StartsWith("RehabCS"))
        {
            this.ResetRehab(15);
        }
        else
        {
            switch (treatmentName)
            {
                case "ChipSeal":
                    this.ResetSeal();
                    break;
                case "ThinAC":
                    this.ResetAC();
                    break;
                case "Project_ChipSeal":
                    this.ResetSeal();
                    break;
                case "Project_ThinAC":
                    this.ResetAC();
                    break;
                case "Project_Rehab_AC":
                    this.SurfClass = "ac";
                    this.ResetRehab(40);
                    this.ACisOK = 1;  //Can do AC overlay now
                    break;
                case "ThinAC_SR":
                    this.SurfClass = "ac";
                    this.ResetRehab(40);
                    this.ACisOK = 1;  //Can do AC overlay now
                    break;
                case "Project_Rehab_CS":
                    this.SurfClass = "seal";
                    this.SurfMaterial = "2CHIP";
                    this.ResetRehab(15);
                    break;
                case "PreSeal_Rep":
                    this.ResetPreSealRepair();
                    break;
                case "BlockPave":
                    this.SurfClass = "block";
                    this.SurfMaterial = "INBLK";
                    this.ResetRehab(150);                    
                    break;
                case "ConcreteRep":
                    this.SurfClass = "concrete";
                    this.SurfMaterial = "CONC";
                    this.ResetRehab(150);                    
                    break;
                case "MaintPA":
                    //Only increment surface age and pavement age
                    // For now we assume maintenance does not reset any distresses, but it does 'arrest' it for this year
                    this.PavementAge++;
                    this.SurfAge++;
                    break;
                case "MaintSU":
                    // For now we assume maintenance does not reset any distresses, but it does 'arrest' it for this year
                    this.PavementAge++;
                    this.SurfAge++;
                    break;
                default:
                    //should not get here unless treatment is some form of rehab
                    if (treatmentName.StartsWith("Rehab"))
                    {
                        throw new Exception($"Treatent type '{treatmentName}' is not handled in Resetter");
                    }
                    break;
            }
        }


        //Calculate and update Expected Surface life, but only if it is not maintenance
        if (treatmentName != "MaintPA" && treatmentName != "MaintSU")
        {            
            this.SurfLifeExpected = this.SetupInfo.SurfLifeExpLookup[this.GetKeyForSurfLifeExpectedLookup()];            
        }
        
    }

    public List<TreatmentStrategy> GetStrategies(int ielem, int iPeriod, double periodsToNextTreatment)
    {
        List<TreatmentStrategy> strategies = new List<TreatmentStrategy>();
        //Only consider treatments if the time/periods to the next treatment is large enough
        //if (periodsToNextTreatment > this.SetupInfo.GapToNextTreatment)
        //{
        //    if (this.IsSecondCoatSituation)
        //    {
        //        strategies.Add(GetSecondCoatStrategy(ielem, iPeriod, rawRow, prevValues, this.AreaM2));
        //    }
        //    else if (this.CanConsiderTreatment(PDI_threshold_entry, SDI_threshold_entry, WaitTimeBetweenTreatments))
        //    {
        //        if (this.ConsiderAsphaltTreatment(model))
        //        {
        //            if (this.CanConsiderRehab(RehabMinLength, RehabMaxLength, PDI_threshold_rehab_ac, MSDRemLifeThresholdRehab))
        //            {
        //                strategies.Add(GetRehabACStrategy(ielem, iPeriod, rawRow, prevValues, this.AreaM2, segment));
        //            }

        //            double canDoAC = prevValues[PIndex("par_ac_ok")];

        //            if (canDoAC > 0)
        //            {
        //                strategies.Add(GetThinACwithRepairStrategy(ielem, iPeriod, rawRow, prevValues, this.AreaM2, segment));

        //                TreatmentStrategy strat2 = new TreatmentStrategy(ielem, rawRow, prevValues, iPeriod);
        //                strat2.AddFirstTreatment("ThinAC", this.AreaM2, $"pdi = {this.PDI}; sdi = {this.SDI}", "none");
        //                strat2.AddFollowUpTreatment("ThinAC", 8, this.AreaM2, "none", "none");
        //                strategies.Add(strat2);

        //                TreatmentStrategy strat3 = new TreatmentStrategy(ielem, rawRow, prevValues, iPeriod);
        //                strat3.AddFirstTreatment("ThinAC", this.AreaM2, $"pdi = {this.PDI}; sdi = {this.SDI}", "none");
        //                strat3.AddFollowUpTreatment(GetRehabCode("ac", segment), 8, this.AreaM2, "none", "none");
        //                strategies.Add(strat3);
        //            }

        //        }
        //        else if (this.SurfClass == "block")
        //        {
        //            TreatmentStrategy blockStrat = new TreatmentStrategy(ielem, rawRow, prevValues, iPeriod);
        //            blockStrat.AddFirstTreatment("BlockPave", this.AreaM2, $"pdi = {this.PDI}; sdi = {this.SDI}", "none");
        //            strategies.Add(blockStrat);
        //        }
        //        else if (this.SurfClass == "concrete")
        //        {
        //            TreatmentStrategy concStrat = new TreatmentStrategy(ielem, rawRow, prevValues, iPeriod);
        //            concStrat.AddFirstTreatment("ConcreteRep", this.AreaM2, $"pdi = {this.PDI}; sdi = {this.SDI}", "none");
        //            strategies.Add(concStrat);
        //        }
        //        else
        //        {
        //            //This case handles SEALS but also cases where the existing surface is AC but we are reverting to a seal because NextSurfacing is "seal"
        //            if (this.CanConsiderRehab(RehabMinLength, RehabMaxLength, PDI_threshold_rehab_chip, MSDRemLifeThresholdRehab))
        //            {
        //                TreatmentStrategy strat1 = GetRehabChipSealStrategy(ielem, iPeriod, rawRow, prevValues, this.AreaM2, segment);
        //                strategies.Add(strat1);
        //            }

        //            TreatmentStrategy stratPreseal = new TreatmentStrategy(ielem, rawRow, prevValues, iPeriod);
        //            stratPreseal.AddFirstTreatment("PreSeal_Rep", this.AreaM2, $"pdi = {this.PDI}; sdi = {this.SDI}", "none");
        //            stratPreseal.AddFollowUpTreatment("ChipSeal", 1, this.AreaM2, "Seal after pre-Seal", "none", true);
        //            stratPreseal.AddFollowUpTreatment("ChipSeal", 9, this.AreaM2, "Follow up", "none");
        //            strategies.Add(stratPreseal);

        //            TreatmentStrategy strat2 = new TreatmentStrategy(ielem, rawRow, prevValues, iPeriod);
        //            strat2.AddFirstTreatment("ChipSeal", this.AreaM2, $"pdi = {this.PDI}; sdi = {this.SDI}", "none");
        //            strat2.AddFollowUpTreatment("ChipSeal", 8, this.AreaM2, "Follw up", "none");
        //            strategies.Add(strat2);

        //            TreatmentStrategy strat3 = new TreatmentStrategy(ielem, rawRow, prevValues, iPeriod);
        //            strat3.AddFirstTreatment("ChipSeal", this.AreaM2, $"pdi = {this.PDI}; sdi = {this.SDI}", "none");
        //            strat3.AddFollowUpTreatment(GetRehabCode("seal", segment), 8, this.AreaM2, "Rehab in future", "none");
        //            //ToDo: if forcing later follow up, this may cause duplicate treatment error
        //            strat3.AddFollowUpTreatment("ChipSeal", 9, this.AreaM2, "none", "none");
        //            strategies.Add(strat3);
        //        }
        //    }
        //}
        return strategies;
    }

    #endregion

    #region Utilities - General
     
    protected override void UpdateCalculatedProperties()
    {
        this.SetPDI();
        this.SetSDI();
        this.SetObjective();
    }
    
    #endregion

    #region Utilities - Surfacing/Pavement/Traffic

    private string GetKeyForSurfLifeExpectedLookup()
    {
        switch (this.SurfClass)
        {
            case "concrete":
                return "concrete_undefined";

            case "block":
                return "block_undefined";
            default:

                string key = "unknown";
                if (this.SetupInfo.IncludePavUseInSurfLifeExpKey)
                {
                    key = this.UrbanRural + this.PavementUse + "_" + this.SurfFunction + "_" + this.SurfMaterial;
                }
                else
                {
                    key = this.UrbanRural + "_" + this.SurfFunction + "_" + this.SurfMaterial;
                }

                if (this.SetupInfo.SurfLifeExpLookup.ContainsKey(key) == false)
                {
                    this._model.Warnings.Add($"Surface life lookup key '{key}' is not handled. Assigning default Expected Life");
                    //First try to get a key based on material Class ('ac' or 'sea') using 'undefined' default:
                    key = this.SurfClass + "_undefined";
                    if (this.SetupInfo.SurfLifeExpLookup.ContainsKey(key) == false)
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

    #region Utilities - Treatment Triggering

    public bool IsSecondCoatSituation(ModelBase model)
    {

        if (SurfFunction == "1" && SurfClass == "seal")
        {
            //Get Surfacing age, but add 1 to get the age at the END of THIS year
            double surfAge = Math.Floor(SurfAge + 1);
            double secondCoatWait = this._model.GetLookupValueNumber("second coat", ONRC);
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

    //public bool CanConsiderRehab(double minLengthAllowed, double maxLengthAllowed, double pdiThreshold, double msdRemLifeMaxThreshold)
    //{
    //    if (IsRehabRoute)
    //    {
    //        //First check for disqualifying conditions
    //        if (Length < minLengthAllowed) { return false; }
    //        if (Length > maxLengthAllowed) { return false; }
    //        if (PDI < pdiThreshold) { return false; }

    //        //Now check for qualifying conditions based on MSD remaining life - only do rehab if MSD has data (value NOT minus 1) and
    //        // and the remaining life is below threshold
    //        if (RemainingStructuralLife < 0) { return true; }  //Do not know remaining life (no MSD or FWD)

    //        if (RemainingStructuralLife <= msdRemLifeMaxThreshold) { return true; }
    //        return false;
    //    }
    //    else
    //    {
    //        return false;
    //    }
    //}

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

    public double GetInitialRut()
    {
        
        //Initial rut is maximum of LWP and RWP values
        double rut = Math.Max(this.RutLWP85th, this.RutRWP85th);
        double percRemaining = 1;
        double minAllowed = this._model.GetLookupValueNumber("resets", "rut_reset_min");
        double maxAllowed = this._model.GetLookupValueNumber("resets", "rut_reset_max");
       
        if(this.SurfAge < this.HSDSurveyAge)
        {            
            if (this.SurfFunction == "1")
            {
                return minAllowed;
            }
            else if (this.SurfClass == "ac")
            {
                percRemaining = this._model.GetLookupValueNumber("resets", "ac_rut_perc_post");
            }
            else
            {
                percRemaining = this._model.GetLookupValueNumber("resets", "seal_rut_perc_post");
            }
        }
        return Math.Clamp(rut * percRemaining, minAllowed, maxAllowed);
    }

    public double GetInitialNaasra()
    {
        double percRemaining = 1;

        double minAllowed = this._model.GetLookupValueNumber("resets", "naasra_reset_min");
        double maxAllowed = this._model.GetLookupValueNumber("resets", "naasra_reset_max");
        
        if (this.SurfAge < this.HSDSurveyAge)
        {            
            if (this.SurfFunction == "1")
            {
                return minAllowed;
            }
            else if (SurfClass == "ac")
            {
                percRemaining = this._model.GetLookupValueNumber("resets", "ac_naasra_perc_post");
            }
            else
            {
                percRemaining = this._model.GetLookupValueNumber("resets", "seal_naasra_perc_post");
            }
        }
        return Math.Clamp(this.Naasra85th * percRemaining, minAllowed, maxAllowed);
    }

    public double GetInitialDistress(double currentValue, string distressKey)
    {            
        double percRemaining = 0;
        if (this.SurfAge < this.VisCondSurveyAge)
        {           
            if (this.SurfFunction == "1")
            {
                return 0;
            }
            else if (this.SurfClass == "ac")
            {
                string key = "ac_" + distressKey + "_reset";
                percRemaining = this._model.GetLookupValueNumber("resets", key);
            }
            else
            {
                string key = "seal_" + distressKey + "_reset";
                percRemaining = this._model.GetLookupValueNumber("resets", key);
            }
        }
        double minAllowed = 0;
        double maxAllowed = 4;
        return Math.Clamp(currentValue * percRemaining, minAllowed, maxAllowed);
    }

    public double GetInitialRemainingStructuralLife()
    {
        //If LWP or RWP MSD remaining life is not known, it will have value -1
                
        //Both lwp and rwp has values
        if (this.MSDRemainingLifeLWP >= 0 && this.MSDRemainingLifeRWP >= 0)
        {
            return Math.Min(this.MSDRemainingLifeLWP, this.MSDRemainingLifeRWP);  //return minimum of the two    
        }
        else if (this.MSDRemainingLifeLWP == -1 && this.MSDRemainingLifeRWP == -1)  //None has values
        {
            return -1;  //Return -1 which indicates no data
        }
        else
        {
            // If we get here, it means only one of lwp or rwp has valid data, find which one
            if (this.MSDRemainingLifeLWP >= 0)
            {
                return this.MSDRemainingLifeRWP;
            }
            if (this.MSDRemainingLifeLWP >= 0)
            {
                return this.MSDRemainingLifeRWP;
            }
            return -1;  //should not get here
        }     
    }

    
    #endregion

    #region Index/Objective Calculation

    private void SetPDI()
    {
        double[] defects = new double[4];
        defects[0] = (double)this.GetParameterValue("par_pct_lt_cracks");
        defects[1] = (double)this.GetParameterValue("par_pct_mesh_cracks");
        defects[2] = (double)this.GetParameterValue("par_pct_shoving");
        defects[3] = (double)this.GetParameterValue("par_pct_potholes");

        if (this.SetupInfo.PdiCalcMethod == "cost354")
        {
            this._pdi = JCass_Functions.Engineering.IndexCalculator.GetCOST354Index(defects, this.SetupInfo.WeightsPDI, 4, 20);
        }
        else
        {
            double dotProduct = defects.Zip(this.SetupInfo.WeightsPDI, (a, b) => a * b).Sum();
            this._pdi = dotProduct;
        }
    }

    private void SetSDI()
    {
        //"", "", "", "", "", ""
        double[] defects = new double[4];
        defects[0] = (double)this.GetParameterValue("par_pct_flushing");
        defects[1] = (double)this.GetParameterValue("par_pct_scabbing");
        defects[2] = (double)this.GetParameterValue("par_pct_mesh_cracks");
        defects[3] = (double)this.GetParameterValue("par_pct_potholes");
        if (this.SetupInfo.SdiCalcMethod == "cost354")
        {
            this._sdi = JCass_Functions.Engineering.IndexCalculator.GetCOST354Index(defects, this.SetupInfo.WeightSDI, 4, 20);
        }
        else
        {
            double dotProduct = defects.Zip(this.SetupInfo.WeightSDI, (a, b) => a * b).Sum();
            this._sdi = dotProduct;
        }
    }

    private void SetObjective()
    {
        //ToDo: Finalise calculation of objective
        double[] defects = new double[3] { this.PDI, this.SDI, 0 };
        double[] tmp_weights1 = new double[3] { this.SetupInfo.WeightsObjective[0], this.SetupInfo.WeightsObjective[1], this.SetupInfo.WeightsObjective[2] };
        double[] tmp_weights2 = JCass_Core.Utils.HelperMethods.NormaliseWeights(tmp_weights1);
        this._objective = JCass_Functions.Engineering.IndexCalculator.GetCOST354Index(defects, tmp_weights2, 4, 20);

        if (this.SetupInfo.ScaleObjectiveByLength) { this._objective = this._objective * this.AreaM2; }        
    }

    #endregion

    #region Reset Helpers

    private void ResetRehab(double surfThicknessNew)
    {
        
        if (this.SurfClass == "ac" || this.SurfClass == "block" || this.SurfClass == "concrete")
        {
            // For AC rehab, update Surface Function to "R" and not "1"
            this.SurfFunction = "R";
        }
        else
        {
            // For Seals, update Surface Function to "1"
            this.SurfFunction = "1";
        }

        this.PavementAge = 0;
        this.SurfAge = 0;
        this.SurfThickness = surfThicknessNew;  //ToDo: Surface thickness after AC/Chip Rehab?
        
        this.SurfLifeExpected = this.SetupInfo.SurfLifeExpLookup[GetKeyForSurfLifeExpectedLookup()];
        
        this.Naasra85th = this.SetupInfo.NaasraMin;
        this.Rut85th = this.SetupInfo.RutMin;

        foreach (string distressParam in this.SetupInfo.DistressParamCodes)
        {            
            this.SetParameterValue(distressParam, 0);
        }

        this.RemainingStructuralLife = this.SetupInfo.ResetRemLifeAfterRehab;        

    }

    private void ResetSeal()
    {
                                
        if (this.SurfFunction == "1")
        {
            this.SurfFunction = "2";
        }
        else
        {
            this.SurfFunction = "R";
        }

        double naasraPercPost = Convert.ToDouble(this._model.Lookups["resets"]["seal_naasra_perc_post"]);
        this.Naasra85th = Math.Clamp(this.Naasra85th * naasraPercPost, this.SetupInfo.NaasraMin, this.SetupInfo.NaasraMax);

        double rutPercPost = Convert.ToDouble(this._model.Lookups["resets"]["seal_rut_perc_post"]);
        this.Rut85th = Math.Clamp(this.Rut85th * rutPercPost, this.SetupInfo.RutMin, this.SetupInfo.RutMax);

        this.SurfAge = 0;
        this.PavementAge++;
        this.SurfLayerCount++;
        this.SurfThickness += 15;

        this.ResetDistressValues("seal");
        
        this.UpdateCalculatedProperties();

    }

    private void ResetPreSealRepair()
    {        
        double naasraPercPost = Convert.ToDouble(this._model.Lookups["resets"]["preseal_naasra_perc_post"]);
        this.Naasra85th = Math.Clamp(this.Naasra85th * naasraPercPost, this.SetupInfo.NaasraMin, this.SetupInfo.NaasraMax);

        double rutPercPost = Convert.ToDouble(this._model.Lookups["resets"]["preseal_rep_rut_perc_post"]);
        this.Rut85th = Math.Clamp(this.Rut85th * rutPercPost, this.SetupInfo.RutMin, this.SetupInfo.RutMax);

        this.SurfAge = 0;
        this.PavementAge++;

        this.ResetDistressValues("preseal");
        
        this.UpdateCalculatedProperties();

    }

    private void GetResetThinACwithStrucRepair()
    {
        if (this.SurfFunction == "1")
        {
            this.SurfFunction = "2";
        }
        else
        {
            this.SurfFunction = "R";
        }

        double naasraPercPost = Convert.ToDouble(this._model.Lookups["resets"]["ac_sr_naasra_perc_post"]);
        this.Naasra85th = Math.Clamp(this.Naasra85th * naasraPercPost, this.SetupInfo.NaasraMin, this.SetupInfo.NaasraMax);

        double rutPercPost = Convert.ToDouble(this._model.Lookups["resets"]["ac_sr_rut_perc_post"]);
        this.Rut85th = Math.Clamp(this.Rut85th * rutPercPost, this.SetupInfo.RutMin, this.SetupInfo.RutMax);

        this.SurfAge = 0;
        this.PavementAge++;
        this.SurfLayerCount++;
        this.SurfThickness += 40;

        this.ResetDistressValues("ac_sr");
        
        this.UpdateCalculatedProperties();
    }

    private void ResetAC()
    {        
        if (this.SurfFunction == "1")
        {
            this.SurfFunction = "2";
        }
        else
        {
            this.SurfFunction = "R";
        }

        this.SurfAge = 0;

        //TODO: make Naasra reset percentage a function of previous Naasra value
        double naasraPercPost = this._model.GetLookupValueNumber("resets", "ac_naasra_perc_post");
        
        this.Naasra85th = Math.Clamp(this.Naasra85th * naasraPercPost, this.SetupInfo.NaasraMin, this.SetupInfo.NaasraMax);

        double rutPercPost = this._model.GetLookupValueNumber("resets", "ac_rut_perc_post");
        this.Rut85th = Math.Clamp(this.Rut85th * rutPercPost, this.SetupInfo.RutMin, this.SetupInfo.RutMax);

        this.PavementAge++;

        this.SurfLayerCount++;
        this.SurfThickness += 40;

        this.ResetDistressValues("ac");
        
        this.UpdateCalculatedProperties();

    }

    private void ResetDistressValues(string resetKeyPrefix)
    {
        foreach (string distressParam in this.SetupInfo.DistressParamCodes)
        {
            double currentValue = (double)this.GetParameterValue(distressParam);
            string key = resetKeyPrefix + "_" + distressParam + "_reset";
            double distressReset = Convert.ToDouble(this._model.Lookups["resets"][key]);
            this.SetParameterValue(distressParam, Math.Min(distressReset, currentValue));
        }
    }
   
    #endregion





}
