using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLModelClasses.DomainClasses;

public class RoadSegmentBase
{
    #region ML Model Training Related

    public bool Target_Classification;

    public Single Target_Regression;

    #endregion

    #region Traffic Related

    public float ADT { get; set; }

    public float HeavyPercent { get; set; }

    #endregion

    #region Surfacing/Pavement Related

    public float SurfLayerNumber { get; set; }

    public String? SurfClass { get; set; }

    public String? SurfFunction { get; set; }

    public float SurfLayerCount { get; set; }

    public float SurfThickness { get; set; }

    public float SurfAge { get; set; }

    public float PavementAge { get; set; }

    #endregion

    #region General
    
    public float Length { get; set; }
       
    public String? UrbanRural { get; set; }

    #endregion

    #region Condition/Distress

    public float Rut85th { get; set; }

    public float Naasra85th { get; set; }

    public float PctFlushing { get; set; }

    public float PctScabbing { get; set; }

    public float PctLTCracks { get; set; }

    public float PctAlligatorCracks { get; set; }

    public float PctShoving { get; set; }

    public float PctPotholes { get; set; }

    #endregion

}
