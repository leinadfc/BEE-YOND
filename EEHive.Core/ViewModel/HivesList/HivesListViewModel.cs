using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EEHive.Word.Core
{
    /// <summary>
    /// A view model for the overview chat list
    /// </summary>
    public class HivesListViewModel : BaseViewModel
    {
        /// <summary>
        /// The chat list items for the list
        /// </summary>
        public List<HivesListItemViewModel> Items { get; set; }
    }
}
