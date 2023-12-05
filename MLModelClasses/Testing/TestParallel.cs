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
using MLModelClasses.MLClasses;

namespace MLModelClasses.Testing
{
    public class TestParallel
    {
        public List<RoadSegmentBase> segments;
        private PredictionEngine<RoadSegmentBase, Prediction_Binary> mlPredictor;

        public void LoadModel(string modelSavePath)
        {
            var mlContext = new MLContext();

            // Define trained model schemas
            DataViewSchema modelSchema;

            //Load the All-In-One Model (data transformation and prediction in a single pipeline)
            ITransformer allInOneModel = mlContext.Model.Load(modelSavePath, out modelSchema);

            //Create the prediction engine
            mlPredictor = mlContext.Model.CreatePredictionEngine<RoadSegmentBase, Prediction_Binary>(allInOneModel);
            
        }

        public void LoadData(string dataFileePath) 
        {
        
            this.segments = RoadSegmentLoader.LoadRoadSegmentsFromCsv(dataFileePath);
        
        }


        public double Predict(RoadSegmentBase segment)
        {
            var prediction = this.mlPredictor.Predict(segment);
            return prediction.Probability;
        }

        public double Predict_Serie()
        {            
            double sum = 0;
            foreach (RoadSegmentBase segment in this.segments)
            {

                sum += this.Predict(segment);
            }
            return sum;            
        }

        public double Predict_Parallel1()
        {
            double sum = 0;
            Parallel.ForEach(this.segments, segment =>
            {
                sum += this.Predict(segment);
            });
            return sum; 
        }

        public double Predict_Parallel()
        {
            double sum = 0;
            Parallel.For(0, this.segments.Count, iElem =>
            {
                sum += this.Predict(this.segments[iElem]);

            });
            return sum;
        }



        

    }
}
