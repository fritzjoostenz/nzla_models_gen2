using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLModelClasses.MLClasses;

public class Prediction_Binary
{

    // Original label.
    public bool PredictedLabel { get; set; }

    // Predicted score from the trainer.
    public float Score { get; set; }

    // Predicted score from the trainer.
    public float Probability { get; set; }

}

