using Microsoft.ML;
using MLModelClasses.DomainClasses;
using NZLAModelBuilder.DataLoaders;
using Org.BouncyCastle.Bcpg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NZLAModelBuilder.Builders;

internal static class BuilderUtilities
{

    public static Dictionary<string, IEstimator<ITransformer>> GetCandidateModels_Logistic(MLContext mlContext)
    {
       
        Dictionary<string, IEstimator<ITransformer>> models = new Dictionary<string, IEstimator<ITransformer>>();

        models.Add("lbfgsLogReg", mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression());
        models.Add("FastTree", mlContext.BinaryClassification.Trainers.FastTree());
        models.Add("FieldAwareFM", mlContext.BinaryClassification.Trainers.FieldAwareFactorizationMachine());
        models.Add("LightGBM", mlContext.BinaryClassification.Trainers.LightGbm());

        //Works but slow and never the best:
        //models.Add("Sdca", mlContext.BinaryClassification.Trainers.SdcaLogisticRegression());            
        //models.Add("GAM", mlContext.BinaryClassification.Trainers.Gam());

        //Cannot get these to work in Cross-Validation:
        //models.Add("LinearSVM", mlContext.BinaryClassification.Trainers.LinearSvm());
        //models.Add("AvgPerceptron", mlContext.BinaryClassification.Trainers.AveragedPerceptron());
        //models.Add("FastForest", mlContext.BinaryClassification.Trainers.FastForest());
        //models.Add("LDSVM", mlContext.BinaryClassification.Trainers.LdSvm());
        return models;
    }

    public static void BuildModel(string workFolder, string distressName)
    {
        string in_file = workFolder + "input_data_imputed.csv";
        List<RoadSegmentBase> segments = RoadSegmentLoader.LoadRoadSegmentsFromCsv(in_file);

        ClassificationModelBuilderBase modelBuilder;
        switch (distressName)
        {
            case "flushing":
                modelBuilder = new FlushingModelBuilder(workFolder, segments);                
                break;
            case "scabbing":
                modelBuilder = new ScabbingModelBuilder(workFolder, segments);
                break;
            case "lt_cracks":
                modelBuilder = new LTCracksModelBuilder(workFolder, segments);
                break;
            case "mesh_cracks":
                modelBuilder = new MeshCracksModelBuilder(workFolder, segments);
                break;
            case "shoving":
                modelBuilder = new ShovingModelBuilder(workFolder, segments);
                break;
            case "pothole":
                modelBuilder = new PotholeModelBuilder(workFolder, segments);
                break;
            case "rutting":
                modelBuilder = new RuttingModelBuilder(workFolder, segments);
                break;
            case "naasra":
                modelBuilder = new NaasraModelBuilder(workFolder, segments);
                break;
            default:
                throw new Exception($"Distress '{distressName}' is not handled.");
        }

        modelBuilder.LogConsoleLine($"Doing Cross Validation for {distressName} model:");
        modelBuilder.DoCrossValidationAndSetBestModel();

        modelBuilder.LogConsoleLine("");
        modelBuilder.LogConsoleLine("-------------------------------------------------------------------------------");
        modelBuilder.LogConsoleLine($"Training and Testing Best {distressName} model on holdout test set:");
        modelBuilder.TrainAndTestBestModel(false);

        modelBuilder.LogConsoleLine("");
        modelBuilder.LogConsoleLine("-------------------------------------------------------------------------------");
        modelBuilder.LogConsoleLine($"Training {distressName} model on all data and saving the model zip file:");
        modelBuilder.TrainAndSaveFinalModelOnAllData();

        modelBuilder.LogConsoleLine("");
        modelBuilder.LogConsoleLine("-------------------------------------------------------------------------------");
        modelBuilder.LogConsoleLine($"Loading Saved {distressName} model and making predictions on all data:");
        modelBuilder.LoadModelAndMakeNewPredictions();

        modelBuilder.WriteLinesToFile();

    }


}
