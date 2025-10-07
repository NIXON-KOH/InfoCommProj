using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace TryMeBitch.Models
{
    public class BrakePressureLogsViewModel
    {
        // Dropdown options
        public List<SelectListItem> TrainOptions { get; set; }
        // Holds the currently-selected train ID
        public string SelectedTrain { get; set; }
        // The logs to display
        public List<BrakePressureLog> Logs { get; set; }
    }
}
