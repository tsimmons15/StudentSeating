using System;
using System.Collections.Generic;
using System.Windows;

namespace StudentSeating.Models
{
    [Serializable]
    public class Section
    {

        public int Id
        {
            get; set;
        }

        public String Title
        {
            get; set;
        }

        public int RowCount
        {
            get; set;
        }

        public int MaxRow
        {
            get; set;
        }

        public Section()
        {
        }
    }
}
