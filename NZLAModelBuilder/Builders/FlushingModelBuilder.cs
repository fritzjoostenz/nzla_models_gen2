using Microsoft.ML;
using MLModelClasses.DomainClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NZLAModelBuilder.Builders
{
    internal class FlushingModelBuilder: ClassificationModelBuilderBase
    {

        public FlushingModelBuilder(string workFolder, List<RoadSegmentBase> segments, Double testDataFraction = 0.25)
        {            
            this.WorkFolder = workFolder;

            this.SetupFilePaths("flushing");
                       
            this.Segments = segments;
            foreach (var segment in segments)
            {
                segment.Target_Classification = false;
                if (segment.PctFlushing > 0) { segment.Target_Classification = true; }
            }

            this.SetupMLObjects(testDataFraction);
        }

        protected override IEstimator<ITransformer> GetModelSetupPipeLine()
        {
            var pipeline = mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: nameof(RoadSegmentBase.Target_Classification))
                                     .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "surf_class_code", inputColumnName: nameof(RoadSegmentBase.SurfClass)))
                                     .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "urban_rural_code", inputColumnName: nameof(RoadSegmentBase.UrbanRural)))
                                     .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "SurfFunc_code", inputColumnName: nameof(RoadSegmentBase.SurfFunction)))
                                     .Append(mlContext.Transforms.Concatenate("Features", "surf_class_code", "urban_rural_code", "SurfFunc_code", nameof(RoadSegmentBase.ADT),
                                     nameof(RoadSegmentBase.HeavyPercent), nameof(RoadSegmentBase.SurfThickness), nameof(RoadSegmentBase.SurfLayerCount), nameof(RoadSegmentBase.PavementAge),
                                     nameof(RoadSegmentBase.SurfAge), nameof(RoadSegmentBase.Length)));
            return pipeline;
        }

    }
}
