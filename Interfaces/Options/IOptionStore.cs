using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace StudentSeating.Interfaces
{
    public interface IOptionStore: INotifyPropertyChanged
    {
        public int OptionUpdateRate { get; set; }
        public int ExamUpdateRate { get; set; }
        public int SeatingUpdateRate { get; set; }
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }
        public Double FontSize { get; set; }
        public string ExamFilePath { get; set; }
        public int ConnectionRetries { get; set; }
        public int SeatSpacing { get; set; }

        //Theme settings?

        //Seating Mode {Plague, Normal, etc...}

        public Action OptionMonitoring { get; set; }

        public void StartOptionUpdate(Action? callback = null);

        /// <summary>
        /// Handles the safe closure of the option storage container.
        /// </summary>
        /// <returns>Completed task</returns>
        public Task Close();

        public void LoadOptions();

        public void SaveOptions();
    }
}
