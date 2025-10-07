using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace TryMeBitch.Models
{
    public class CabinTempLogsViewModel
    {
        // Dropdown
        public List<SelectListItem> TrainOptions { get; set; }
        public string SelectedTrain { get; set; }
        // Table data
        public List<CabinTempLog> Logs { get; set; }
    }
}
