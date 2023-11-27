// See https://aka.ms/new-console-template for more information


using MLModelClasses.DomainClasses;
using NZLAModelBuilder.Builders;
using NZLAModelBuilder.DataLoaders;
using System.Reflection.Emit;


string workFolder = @"C:\Users\fritz\Juno Services Dropbox\Local_Authorities\aa_gen2_models\jcass\";

BuilderUtilities.BuildModel(workFolder, "flushing");

BuilderUtilities.BuildModel(workFolder, "scabbing");

BuilderUtilities.BuildModel(workFolder, "lt_cracks");

BuilderUtilities.BuildModel(workFolder, "mesh_cracks");

BuilderUtilities.BuildModel(workFolder, "shoving");

BuilderUtilities.BuildModel(workFolder, "pothole");

