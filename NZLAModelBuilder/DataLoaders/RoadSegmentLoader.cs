using Microsoft.VisualBasic.FileIO;
using MLModelClasses.DomainClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NZLAModelBuilder.DataLoaders;

internal static class RoadSegmentLoader
{

    internal static List<RoadSegmentBase> LoadRoadSegmentsFromCsv(string filePath, string surveyDateColumnName = "cond_survey_date")
    {
        List<RoadSegmentBase> segments = new List<RoadSegmentBase>();

        using (TextFieldParser parser = new TextFieldParser(filePath))
        {
            // Set up the parser
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");

            // Read the header row to get column names
            if (!parser.EndOfData)
            {
                string[] headers = parser.ReadFields();
                Dictionary<string, int> columnIndex = new Dictionary<string, int>();

                // Map column names to their indexes
                for (int i = 0; i < headers.Length; i++)
                {
                    columnIndex[headers[i]] = i;
                }

                // Process the remaining rows
                while (!parser.EndOfData)
                {
                    string[] fields = parser.ReadFields();

                    string surfDate = Convert.ToString(fields[columnIndex["surf_date"]]);
                    string paveDate = Convert.ToString(fields[columnIndex["pave_date"]]);
                    string surveyDate = Convert.ToString(fields[columnIndex[surveyDateColumnName]]);

                    // Create a new RoadSegmentBase object and add it to the list
                    RoadSegmentBase RoadSegmentBase = new RoadSegmentBase
                    {
                        ADT = Convert.ToSingle(fields[columnIndex["adt"]]),
                        HeavyPercent = Convert.ToSingle(fields[columnIndex["heavy_perc"]]),
                        
                        SurfLayerNumber = Convert.ToSingle(fields[columnIndex["surf_layer_no"]]),
                        SurfClass = Convert.ToString(fields[columnIndex["surf_class"]]),
                        SurfFunction = Convert.ToString(fields[columnIndex["surf_function"]]),
                        SurfLayerCount = Convert.ToSingle(fields[columnIndex["surf_layer_no"]]),
                        SurfThickness = Convert.ToSingle(fields[columnIndex["surf_thick"]]),
                        SurfAge = GetAgeAtSurveyDate(surfDate, surveyDate),
                        
                        PavementAge = GetAgeAtSurveyDate(paveDate, surveyDate),

                        Length = Convert.ToSingle(fields[columnIndex["length"]]),
                        UrbanRural = Convert.ToString(fields[columnIndex["urban_rural"]]),
                        
                        Rut85th = (Convert.ToSingle(fields[columnIndex["rut_lwpmean_85"]]) + Convert.ToSingle(fields[columnIndex["rut_rwpmean_85"]]))/2,
                        Naasra85th = Convert.ToSingle(fields[columnIndex["naasra_85"]]),

                        PctFlushing = Convert.ToSingle(fields[columnIndex["pct_flush"]]),
                        PctScabbing = Convert.ToSingle(fields[columnIndex["pct_scabb"]]),
                        PctLTCracks = Convert.ToSingle(fields[columnIndex["pct_lt_crax"]]),
                        PctAlligatorCracks = Convert.ToSingle(fields[columnIndex["pct_allig"]]),
                        PctShoving = Convert.ToSingle(fields[columnIndex["pct_shove"]]),
                        PctPotholes = Convert.ToSingle(fields[columnIndex["pct_poth"]]),

                    };

                    if (RoadSegmentBase.SurfAge > 0)
                    {
                        segments.Add(RoadSegmentBase);
                    }                    
                }
            }
        }

        return segments;
    }

    internal static Single GetAgeAtSurveyDate(string ageDateText, string surveyDateText)
    {
        DateTime? ageDate = DateTime.Parse(ageDateText);
        DateTime? surveyDate = DateTime.Parse(surveyDateText);
        if (ageDate is null) { throw new ArgumentNullException($"Cannot parse date from Age Date '{ageDateText}'"); }
        if (surveyDate is null) { throw new ArgumentNullException($"Cannot parse date from Survey Date '{surveyDate}'"); }

        TimeSpan timeSpan = (TimeSpan)(surveyDate - ageDate);
        return (float)(timeSpan.TotalDays / 364.25);


    }

}
