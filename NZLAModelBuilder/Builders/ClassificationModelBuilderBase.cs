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
using JCass_Data.Objects;
using MathNet.Numerics.Random;
using Microsoft.ML;
using Microsoft.ML.Data;
using MLModelClasses.DomainClasses;
using NPOI.SS.Formula.Functions;
using static NPOI.HSSF.Util.HSSFColor;
using NPOI.SS.UserModel;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection;
using MLModelClasses.MLClasses;

namespace NZLAModelBuilder.Builders;

internal abstract class ClassificationModelBuilderBase
{
    #region Properties

    protected string WorkFolder;

    protected string ModelFileSavePath;

    protected string EvaluationFileSavePath;

    protected string LogItemsFileSavePath;

    protected MLContext mlContext;

    protected DataOperationsCatalog.TrainTestData dataSplit;

    protected List<RoadSegmentBase> Segments;

    protected IDataView allDataObserved;

    protected Dictionary<string, IEstimator<ITransformer>> candidateModels;

    protected IEstimator<ITransformer> modelPipeline;

    protected IEstimator<ITransformer> bestModel;

    protected List<string> ConsoleLines = new List<string>();

    #endregion

    #region Public Methods
    
    public void DoCrossValidationAndSetBestModel()
    {      
        ITransformer dataPrepTransformer = this.modelPipeline.Fit(this.allDataObserved);
        IDataView transformedData = dataPrepTransformer.Transform(this.allDataObserved);
                
        string bestModel = "none";
        double bestModelAvgF1 = -1;
        string txt;
        foreach (string modelName in this.candidateModels.Keys)
        {
            IEstimator<ITransformer> trainer = this.candidateModels[modelName];

            var trainedModel = trainer.Fit(transformedData);
            var cvResults = mlContext.BinaryClassification.CrossValidate(transformedData, trainer, numberOfFolds: 10);

            IEnumerable<double> cvMetrics = cvResults.Select(fold => fold.Metrics.F1Score);
            double totalF1 = cvMetrics.Sum();
            double avgF1 = totalF1 / cvMetrics.Count();
            double minF1 = cvMetrics.Min();
            double maxF1 = cvMetrics.Max();

            txt = $"{modelName} Results: Min F1 = {Math.Round(minF1, 2)}; Avg F1 = {Math.Round(avgF1, 2)}; Max F1 = {Math.Round(maxF1, 2)}";
            this.LogConsoleLine(txt);
            

            if (avgF1 > bestModelAvgF1)
            {
                bestModel = modelName;
                bestModelAvgF1 = avgF1;
            }
        }

        txt = $"Best model is '{bestModel}';";
        this.ConsoleLines.Add(txt);
        this.LogConsoleLine(txt);

        this.bestModel = this.candidateModels[bestModel];
    }

    public void TrainAndTestBestModel(bool useAllData)
    {                        
        var trainingPipeline = this.modelPipeline.Append(this.bestModel);

        Microsoft.ML.Data.TransformerChain<ITransformer> trainedModelWithPreproc;
        IDataView predictedTestData;
        IEnumerable<RoadSegmentBase> testData;

        if (useAllData)
        {
            this.LogConsoleLine($"Using ALL available data for Training and Testing:;");
            trainedModelWithPreproc = trainingPipeline.Fit(this.allDataObserved);
            predictedTestData = trainedModelWithPreproc.Transform(this.allDataObserved);
        }
        else
        {
            this.LogConsoleLine($"Using Train/Test split for Training and Testing:;");
            trainedModelWithPreproc = trainingPipeline.Fit(dataSplit.TrainSet);
            predictedTestData = trainedModelWithPreproc.Transform(dataSplit.TestSet);           
        }
               
        var metrics = mlContext.BinaryClassification.Evaluate(predictedTestData);
        this.PrintBinaryClassificationMetrics(metrics);

    }

    public void TrainAndSaveFinalModelOnAllData()
    {       
        var trainingPipeline = this.modelPipeline.Append(this.bestModel);
        var trainedModelWithPreproc = trainingPipeline.Fit(this.allDataObserved);

        //Save the model
        this.LogConsoleLine("Saving model pipeline");        
        mlContext.Model.Save(trainedModelWithPreproc, dataSplit.TrainSet.Schema, ModelFileSavePath);
        this.LogConsoleLine($"Saved model is at: {ModelFileSavePath}");
    }

    public void LoadModelAndMakeNewPredictions()
    {
        var mlContext = new MLContext();
                
        // Define trained model schemas
        DataViewSchema modelSchema;

        //Load the All-In-One Model (data transformation and prediction in a single pipeline)
        ITransformer allInOneModel = mlContext.Model.Load(this.ModelFileSavePath, out modelSchema);
            
        //Create the prediction engine
        var engine = mlContext.Model.CreatePredictionEngine<RoadSegmentBase, Prediction_Binary>(allInOneModel);

        IDataView predictedTestData = allInOneModel.Transform(this.allDataObserved);
                
        List<Prediction_Binary> predictions = mlContext.Data.CreateEnumerable<Prediction_Binary>(predictedTestData, reuseRowObject: false).ToList();

        this.LogConsoleLine($"Metrics for '{this.ModelFileSavePath}' model:");
        // Evaluate the overall metrics            
        var metrics = mlContext.BinaryClassification.Evaluate(predictedTestData);
        this.PrintBinaryClassificationMetrics(metrics);


        jcDataSet output = new jcDataSet();
        output.AddColumn("Observed", "text");
        output.AddColumn("Predicted", "text");
        output.AddColumn("Probability", "number");

        int iRow = 0;
        foreach (RoadSegmentBase segment in this.Segments)
        {
            Dictionary<string, object> row = new Dictionary<string, object>();  
            var prediction = engine.Predict(segment);
            row.Add("Observed", segment.Target_Classification);
            row.Add("Predicted", prediction.PredictedLabel);
            row.Add("Probability", prediction.Probability);

            output.AddRow(row);
        }
                       
        ExcelHelper.ExportDataToExcelSheet(this.EvaluationFileSavePath, "predictions", output);

        this.LogConsoleLine("Finished adding predictions on new data set;");
        
    }

    public void LogConsoleLine(string txt)
    {
        this.ConsoleLines.Add(txt);
        Console.WriteLine(txt);
    }

    public void WriteLinesToFile()
    {
        try
        {
            // Write the lines to the file
            File.WriteAllLines(LogItemsFileSavePath, this.ConsoleLines);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    

    #endregion

    #region Private Methods

    protected void PrintBinaryClassificationMetrics(BinaryClassificationMetrics metrics)
    {
        this.LogConsoleLine($"Accuracy: {metrics.Accuracy:F2}");
        
        this.LogConsoleLine($"AUC: {metrics.AreaUnderRocCurve:F2}");
        this.LogConsoleLine($"F1 Score: {metrics.F1Score:F2}");
        this.LogConsoleLine($"Negative Precision: " + $"{metrics.NegativePrecision:F2}");

        this.LogConsoleLine($"Negative Recall: {metrics.NegativeRecall:F2}");
        this.LogConsoleLine($"Positive Precision: " + $"{metrics.PositivePrecision:F2}");

        this.LogConsoleLine($"Positive Recall: {metrics.PositiveRecall:F2}\n");
        this.LogConsoleLine(metrics.ConfusionMatrix.GetFormattedConfusionTable());

    }

    protected abstract IEstimator<ITransformer> GetModelSetupPipeLine();
        
    protected void SetupFilePaths(string distressCode)
    {
        this.LogConsoleLine("      ");
        this.LogConsoleLine($"--------------- working on model: {distressCode.ToUpper()}  -------------------------");
        this.LogConsoleLine("      ");
        this.LogItemsFileSavePath = Path.Combine(this.WorkFolder, $"ML_Models/evaluation/{distressCode}_model_metrix.txt");
        this.ModelFileSavePath = Path.Combine(this.WorkFolder, $"ML_Models/{distressCode}_model.zip");
        this.EvaluationFileSavePath = Path.Combine(this.WorkFolder, $"ML_Models/evaluation/{distressCode}_model.xlsx");
    }

    protected void SetupMLObjects(double TestDataFraction)
    {
        this.mlContext = new MLContext(12345);
        this.modelPipeline = this.GetModelSetupPipeLine();
        this.candidateModels = BuilderUtilities.GetCandidateModels_Logistic(this.mlContext);
        this.allDataObserved = mlContext.Data.LoadFromEnumerable(this.Segments);
        this.dataSplit = mlContext.Data.TrainTestSplit(this.allDataObserved, TestDataFraction);
    }
    
    #endregion


}
