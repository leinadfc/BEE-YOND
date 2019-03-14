using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EEHive.Word.Core
{
    /// <summary>
    /// The design-time data for a <see cref="HivesListViewModel"/>
    /// </summary>
    public class HivesListDesignModel : HivesListViewModel
    {
        #region Singleton

        /// <summary>
        /// A single instance of the design model
        /// </summary>
        public static HivesListDesignModel Instance => new HivesListDesignModel();

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public HivesListDesignModel()
        {
            Items = new List<HivesListItemViewModel>
            {
                new HivesListItemViewModel
                {
                    Name = "Demo Hive",
                    IsSelected = true
                },
                new HivesListItemViewModel
                {
                    Name = "New Hive 1  -  (No data available)",
                },
            };
        }

        #endregion
    }

}
