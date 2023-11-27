using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLClasses;

// Class used to capture predictions.
public class Prediction_MultiLabel
{
    // Original label.
    public uint Label { get; set; }

    // Predicted label from the trainer.
    public uint PredictedLabel { get; set; }

}

