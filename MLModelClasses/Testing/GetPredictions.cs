using MLModelClasses.DomainClasses;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Drawing;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML;
using Microsoft.ML.Data;
using NPOI.SS.Formula.Functions;
using static NPOI.HSSF.Util.HSSFColor;
using NPOI.SS.UserModel;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection;
using System.Collections.Concurrent;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using MLModelClasses.MLClasses;

namespace MLModelClasses.Testing
{
    public class GetPredictions
    {
        public List<RoadSegmentBase> Segments;        
        private Dictionary<string, PredictionEngine<RoadSegmentBase, Prediction_Binary>> PredictionModels;
        private List<string> DistressCodes = new List<string>() 
        { 
            "pct_flush",
            "pct_scabb",
            "pct_lt_crax",
            "pct_allig",
            "pct_shove",
            "pct_poth"
        };

        private List<string> ModelNames = new List<string>()
        {
            "flushing_model.zip",
            "scabbing_model.zip",
            "lt_cracks_model.zip",
            "mesh_cracks_model.zip",
            "shoving_model.zip",
            "potholes_model.zip"            
        };

        public void LoadModels(string modelsFolderPath)
        {
            var mlContext = new MLContext();                        
            this.PredictionModels = new Dictionary<string, PredictionEngine<RoadSegmentBase, Prediction_Binary>>();
            for (int i = 0; i < DistressCodes.Count; i++)
            {
                string modelSavePath = Path.Combine(modelsFolderPath, this.ModelNames[i]);
                DataViewSchema modelSchema;
                ITransformer allInOneModel = mlContext.Model.Load(modelSavePath, out modelSchema);
                PredictionEngine<RoadSegmentBase, Prediction_Binary> mlPredictor = mlContext.Model.CreatePredictionEngine<RoadSegmentBase, Prediction_Binary>(allInOneModel);
                this.PredictionModels[this.DistressCodes[i]] = mlPredictor;
            }                        
        }
        
        public double PredictProbability(RoadSegmentBase segment, PredictionEngine<RoadSegmentBase, Prediction_Binary> mlPredictor)
        {
            var prediction = mlPredictor.Predict(segment);
            return prediction.Probability;
        }

        public void PredictAndExport(List<RoadSegmentBase> segments, string outputFilePath)
        {
            Console.WriteLine("Starting...");
            this.Segments = segments;
            List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();
            foreach (RoadSegmentBase segment in this.Segments)
            {                
                Dictionary<string, object> row = new Dictionary<string, object>();
                row["surf_age"] = segment.SurfAge;
                row["pave_age"] = segment.PavementAge;
                row["surf_class"] = segment.SurfClass;
                row["urban_rural"] = segment.UrbanRural;
                row["adt"] = segment.ADT;
                row["heavy_perc"] = segment.HeavyPercent;

                foreach (var item in this.DistressCodes)
                {
                    row[item + "_obs"] = this.GetDistressValue(segment, item);                 
                }

                foreach (var item in this.DistressCodes)
                {
                    PredictionEngine<RoadSegmentBase, Prediction_Binary> mlPredictor = this.PredictionModels[item];
                    row[item + "_proba"] = this.PredictProbability(segment, mlPredictor);
                }

                results.Add(row);
            }    
            
            JCass_Data.Utils.CSVHelper.ExportToCsv(results, outputFilePath);
            Console.WriteLine("Done!");

        }
       
        private double GetDistressValue(RoadSegmentBase segment, string distressCode)
        {
            switch (distressCode)
            {
                case "pct_flush": return segment.PctFlushing;
                case "pct_scabb": return segment.PctScabbing;
                case "pct_lt_crax": return segment.PctLTCracks;
                case "pct_allig": return segment.PctAlligatorCracks;
                case "pct_shove": return segment.PctShoving;
                case "pct_poth": return segment.PctPotholes;
                default:
                    throw new NotImplementedException($"Distress code '{distressCode}' is not handled"); 
            }
        }


        public void GetProbabilityVsAge(List<RoadSegmentBase> segments, string outputFilePath)
        {
            Console.WriteLine("Starting...");
            this.Segments = segments;
                        
            List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();

            List<string> urb_rurs = new List<string>() { "U", "R" };
            List<string> surf_classes = new List<string>() { "seal", "ac" };

            for (int i = 0; i < segments.Count; i++)
            {
                RoadSegmentBase segment = segments[i];
                foreach (var distressCode in this.DistressCodes)
                {
                    PredictionEngine<RoadSegmentBase, Prediction_Binary> mlPredictor = this.PredictionModels[distressCode];
                    foreach (var ur in urb_rurs)
                    {
                        foreach (var sc in surf_classes)
                        {
                            for (int j = 1; j <= 20; j++)
                            {
                                segment.SurfAge = j;
                                segment.SurfClass = sc;
                                segment.UrbanRural = ur;
                                double proba = this.PredictProbability(segment, mlPredictor);

                                Dictionary<string, object> row = new Dictionary<string, object>();
                                row["seg_index"] = i;
                                row["distress"] = distressCode;
                                row["surf_age"] = segment.SurfAge;
                                row["urban_rural"] = segment.UrbanRural;
                                row["surf_class"] = segment.SurfClass;
                                row["adt"] = segment.ADT;
                                row["probability"] = proba;

                                results.Add(row);
                            }
                        }
                    }
                }
            }

            

            File.Delete(outputFilePath);
            JCass_Data.Utils.CSVHelper.ExportToCsv(results, outputFilePath);
            Console.WriteLine("Done!");

        }



    }
}
