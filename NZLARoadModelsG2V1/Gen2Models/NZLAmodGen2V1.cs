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
using JCass_ModelCore.DomainModels;
using NZLARoadModelsG2V1.DomainObjects;

using JCass_Data.Objects;
using JCass_Data.Utils;
using JCass_Functions;
using JCass_Functions.Engineering;

namespace JCass_CustomiserSample.Gen2;

/// <summary>
/// Second Generation Model, First Version for New Zealand Local Authorities
/// Simplified model that models distress percentages rather than normalised FM distresses
/// </summary>
public class NZLAmodGen2V1 : DomainModelBase, IDomainModel
{

    #region Variables
    
        
    #endregion
        
    #region Constructor/Setup

    public NZLAmodGen2V1()
    {


    }

    public override void SetupInstance()
    {

         
    }

    #endregion

    #region Interface Implementation

    public override double[] Initialise(int iElemIndex, string[] rawRow)
    {        
        Dictionary<string, object> values = this.model.GetParametersForJFunctions(iElemIndex, rawRow, null, 0);
                
        model.FunctionSet.Evaluate(values, "initialise");
        double[] newValues = this.model.GetModelParameterValuesFromJFunctionResultSet(new double[this.model.NParameters], values);

        return newValues;
    }

    public override double[] InitialiseForCalibration(int iElemIndex, string[] rawRow)
    {
        ////First get usual initialisation values for all parameters
        //newValues = Initialise(rawRow, newValues);

        //RoadModSegmentV1 segment = RoadModSegmentV1.LoadFromRawData(model, rawRow);
        ////Now adjust only those parameters for initial condition

        //newValues[PIndex("par_surf_age")] = 0;
        //newValues[PIndex("par_pav_age")] = Math.Max(0, segment.PavementAge - segment.SurfAge);
        //newValues[PIndex("par_surf_life_ach")] = 0;

        ////Discount traffic to year when surface age was zero. Use smaller historical traffic growth and
        ////also guard against values becoming ridiculously small
        //double historicalGrowthRate = model.GetLookupValueNumber("traffic", "historical_growth_rate_perc");
        //double discFactor = Math.Pow(1 + historicalGrowthRate / 100, -segment.SurfAge);
        //newValues[PIndex("par_adt")] = Math.Max(50, segment.ADT * discFactor);
        //newValues[PIndex("par_heavy")] = Math.Max(1, segment.HeavyVehicles * discFactor);

        //foreach (string distress in distressParams)
        //{
        //    newValues[PIndex(distress)] = 0;
        //}
                
        //double resetRutAssumed = 3.5;
        //double naasraMin = model.GetLookupValueNumber("resets", "naasra_reset_min");
        //double rutMin = model.GetLookupValueNumber("resets", "rut_reset_min");

        //if (segment.SurfFunction == "1")
        //{
        //    newValues[PIndex("par_naasra")] = naasraMin;
        //    newValues[PIndex("par_rut")] = resetRutAssumed;
        //}
        //else if (segment.SurfClass == "ac")
        //{
        //    newValues[PIndex("par_naasra")] = Math.Max(naasraMin, segment.Naasra85th * 0.5);
        //    newValues[PIndex("par_rut")] = Math.Max(rutMin, segment.Rut85th * 0.5);
        //}
        //else
        //{
        //    newValues[PIndex("par_naasra")] = Math.Max(naasraMin, segment.Naasra85th * 0.8);
        //    newValues[PIndex("par_rut")] = Math.Max(rutMin, segment.Rut85th * 0.8);
        //}

        //double pdi = LAsharedGen2.GetPDI(model, newValues, weightsPDI, pdiCalcMethod);
        //double sdi = LAsharedGen2.GetSDI(model, newValues, weightSDI, sdiCalcMethod);
        //newValues[PIndex("par_pdi")] = pdi;
        //newValues[PIndex("par_sdi")] = sdi;
        
        //double rutIndex = rutToIndexConversionModel.GetValue(segment.Rut85th);

        //newValues[PIndex("par_obj")] = GetUpdatedObjective(pdi, sdi, segment);

        return null;
    }

    public override List<TreatmentStrategy> GetStrategies(int ielem, int iPeriod, string[] rawRow, double[] prevValues)
    {
        if (ielem == 12)
        {
            int kk = 9;
        }
        try
        {
            Dictionary<string, object> functionValues = this.model.GetParametersForJFunctions(ielem, rawRow, prevValues, iPeriod);
            model.FunctionSet.Evaluate(functionValues, "triggers");

            List<TreatmentStrategy> strategies = new List<TreatmentStrategy>();
            for (int j = 0; j < this.model.StrategiesSetupData.Count; j++)
            {
                Dictionary<string, object> setupRow = this.model.StrategiesSetupData.Row(j);
                if (TreatmentStrategy.IsTriggered(setupRow, functionValues))
                {
                    TreatmentStrategy strategy = new TreatmentStrategy(ielem, rawRow, prevValues, iPeriod);
                    strategy.SetupFromDefinitionRow(setupRow, functionValues);
                    strategies.Add(strategy);
                    if (strategy.FirstTreatment.Force) { break; }
                }
            }
            if (strategies.Count > 1)
            {
                int kk = 9;
            }
            return strategies;
        }
        catch (Exception ex) 
        { 
            throw new Exception($"Error generating strategies for element {ielem}. Details: {ex.Message}");
        }        
    }

    public override double[] Increment(int iElemIndex, int iPeriod, string[] rawRow, double[] prevValues)
    {
        Dictionary<string, object> values = this.model.GetParametersForJFunctions(iElemIndex, rawRow, prevValues, iPeriod);

        model.FunctionSet.Evaluate(values, "increment");

        double[] newValues = this.model.GetModelParameterValuesFromJFunctionResultSet(new double[this.model.NParameters], values);

        return newValues;

    }

    public override double[] Reset(TreatmentInstance treatment, int iElemIndex, int iPeriod, string[] rawRow, double[] prevValues)
    {
        if (iElemIndex == 12)
        {
            int kk = 9;
        }
        Dictionary<string, object> functionValues = this.model.GetParametersForJFunctions(iElemIndex, rawRow, prevValues, iPeriod, treatment);
        model.FunctionSet.Evaluate(functionValues, "resets");

        double[] newValues = this.model.GetModelParameterValuesFromJFunctionResultSet(new double[this.model.NParameters], functionValues);
        return newValues;
    }

    public override TreatmentInstance GetTriggeredMaintenance(int iElem, int iPeriod, double[] paramValues, string[] rawData)
    {
        
        //double rander = Rando.NextDouble();
        //if (rander < 0.5)
        //{
        //    if (periodsSinceMaint >= maintFreq)
        //    {
        //        RoadModSegment segment = RoadModSegment.LoadFromModelData(model, rawData, paramValues);
        //        segment.PDI = Math.Round(model.GetParameterValue("par_pdi", paramValues), 2);
        //        segment.SDI = Math.Round(model.GetParameterValue("par_sdi", paramValues), 2);
        //        if (segment.PDI > PDI_threshold_maint || segment.SDI > SDI_threshold_maint)
        //        {
        //            string maintType = GetMaintenanceTreatmentName(segment);
        //            if (maintType == "none")
        //            {
        //                return null;
        //            }
        //            else
        //            {
        //                double areaM2 = model.GetRawData_Number(rawData, "area_m2");
        //                TreatmentInstance treatment = new TreatmentInstance(iElem, maintType, iPeriod, areaM2, false, "Routine maintenance", $"pdi = {segment.PDI}; sdi = {segment.SDI}");
        //                treatment.RankParamSimple = segment.PDI;
        //                return treatment;
        //            }
        //        }
        //    }
        //}
        return null;
    }

    #endregion
    
   
}
