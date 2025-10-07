using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace TryMeBitch.Models
{
    public class RFIDEntryLogsViewModel
    {
        // Dropdown
        public List<SelectListItem> TrainOptions { get; set; }
        public string SelectedTrain { get; set; }
        // Table data
        public List<RFIDEntryLog> Logs { get; set; }
    }
}
