using Microsoft.EntityFrameworkCore;
using NLog;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using StudentSeating.Data;
using StudentSeating.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Documents;

namespace StudentSeating.ViewModels
{
    class EmptySeatViewModel : IDialogAware, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private SeatingContext _seatingContext;

        private List<string> _professors;
        public List<string> Professors
        {
            get
            {
                return this._professors;
            }

            set
            {
                this._professors = value;
                NotifyPropertyChanged();
            }
        }
        public List<Exam> Exams
        {
            get
            {
                return this._seatingContext?.Exams.Where((e) => (null == this.Professors) ? false : e.Instructor==this.Professors[0]).ToList();
            }
        }
        
        private Seat contextSeat = null;
        public Seat ContextSeat
        {
            get
            {
                return contextSeat;
            }
            set
            {
                contextSeat = value;
                NotifyPropertyChanged();
            }
        }

        private static string DEFAULT = "None*";
        private static string SAVE_CHANGES = "save";

        public string CalculatorsAllowed 
        { 
            get 
            { 
                if (null != this.SelectedExam)
                {
                    return this.SelectedExam.CalculatorsAllowed;
                }

                return DEFAULT;
            } 
        }

        public string NotesFormulas
        {
            get
            {
                if (null != this.SelectedExam)
                {
                    return this.SelectedExam.NotesFormulas;
                }

                return DEFAULT;
            }
        }

        public string OtherItems
        {
            get
            {
                if (null != this.SelectedExam)
                {
                    return this.SelectedExam.OtherItems;
                }

                return DEFAULT;
            }
        }

        private bool isExamCmbEnabled = false;
        public bool IsExamCmbEnabled {
            get 
            { 
                return isExamCmbEnabled; 
            }
            set 
            { 
                isExamCmbEnabled = value; 
                NotifyPropertyChanged(); 
            } 
        }

        private bool isProfessorCmbEnabled = true;
        public bool IsProfessorCmbEnabled 
        { 
            get 
            { 
                return isProfessorCmbEnabled; 
            } 
            set 
            {
                isProfessorCmbEnabled = value; 
                NotifyPropertyChanged(); 
            } 
        }


        private int selectedIndex = 0;
        public int SelectedIndex 
        { 
            get { 
                return this.selectedIndex; 
            }
            set 
            { 
                selectedIndex = value;
                //For some reason, the combobox binding to SelectedExam does not return proper exam
                // Small workaround, since the index seems to work fine.
                if (selectedIndex >= 0)
                    SelectedExam = professorExams[selectedIndex];
            } 
        }

        private Exam selectedExam = null;
        public Exam SelectedExam
        {
            get
            {
                return selectedExam;
            }
            set
            {
                selectedExam = value;
                if (null != this.ContextSeat)
                    this.ContextSeat.Exam = selectedExam;
                NotifyPropertyChanged();
                NotifyPropertyChanged("OtherItems");
                NotifyPropertyChanged("NotesFormulas");
                NotifyPropertyChanged("CalculatorsAllowed");

            }
        }

        private DateTime timeIn;
        public string TimeIn 
        { 
            get
            {
                return ((timeIn == DateTime.MinValue) ? String.Empty : timeIn.ToString());
            } 
            set
            {
                bool success = DateTime.TryParse(value, out timeIn);

                if (success)
                    NotifyPropertyChanged();
                else
                {
                    Logger logger = LogManager.GetLogger("EmptySeatView.Error");
                    logger.Warn("DateTime {0} unsuccessfully parsed.", value.ToString());
                }
            }
        }

        private int selectedProfessor = 0;
        public int SelectedProfessor 
        { 
            get { return selectedProfessor; } 
            set { 
                selectedProfessor = value; 
                updateExamStatus();
                NotifyPropertyChanged();
            } 
        }

        private List<Exam> professorExams = null;
        public List<Exam> ProfessorExams 
        {
            get 
            { 
                return professorExams; 
            } 
            set 
            { 
                professorExams = value;
                this.SelectedIndex = 0;
                NotifyPropertyChanged(); 
            } 
        }

        private string title;
        public string Title
        {
            get
            {
                return title;
            }

            set
            {
                title = value;
            }
        }

        private string studentName;
        public string StudentName
        {
            get
            {
                return this.studentName;
            }

            set
            {
                this.studentName = value;
                NotifyPropertyChanged();
            }
        }

        private string studentVID;
        public string StudentVID
        {
            get
            {
                return this.studentVID;
            }

            set
            {
                this.studentVID = value;
                NotifyPropertyChanged();
            }
        }

        public event Action<IDialogResult> RequestClose;

        private DelegateCommand<string> _closeDialog;
        public DelegateCommand<string> DialogClose =>
            _closeDialog ?? (_closeDialog = new DelegateCommand<string>(CloseDialog));
        private void CloseDialog(string save)
        {
            //Object can be used to control how we interpret the closing of the window.
            ButtonResult result = ButtonResult.Cancel;

            // The cancel/close options all have command parameter "false". If SeatExam() calls this CloseDialog, it can pass
            //  in "save" to tell us we want to save.
            if ("save" == save)
            {
                result = ButtonResult.OK;
                try
                {
                    this._seatingContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException duce)
                {
                    Logger logger = LogManager.GetLogger("EmptySeatView.Error");
                    logger.Error(duce, "Concurrency issue updating Seats or Section table of the Seating database.");
                }
                catch (DbUpdateException due)
                {
                    Logger logger = LogManager.GetLogger("EmptySeatView.Error");
                    logger.Error(due, "Unable to update tables Seating or Sections of the Seating database.");
                }
                catch (Exception e)
                {
                    Logger logger = LogManager.GetLogger("EmptySeatView.Error");
                    logger.Error(e, "Error, see stacktrace for more information.");
                }
            }
            
            //For instance, I may be able to differentiate between Cancel and 'Save' Button presses.
            RaiseRequestClose(new DialogResult(result));
        }

        public virtual void RaiseRequestClose(IDialogResult dialogResult)
        {
            
            RequestClose?.Invoke(dialogResult);
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {

        }

        
        public void OnDialogOpened(IDialogParameters parameters)
        {
            // In the ViewSeatInfo method of SeatingMainViewModel, passed some parameters
            // Take them, and stash them away
            parameters.TryGetValue<Seat>("Seat", out Seat seat);
            parameters.TryGetValue<SeatingContext>("Context", out this._seatingContext);

            // NotifyOnPropertyChanged needs to fire, but out parameters can't be properties...
            this.ContextSeat = seat;

            // Build the distinct list of professors using the string Instructor parameter included in the exams.
            try
            {
                this.Professors = this._seatingContext?.Exams.Select(e => e.Instructor).Distinct().ToList();
            }
            catch (ArgumentException ae)
            {
                Logger logger = LogManager.GetLogger("EmptySeatView.Error");
                logger.Error(ae, "Linq error building Professor lists.");
            }

            if (this.ContextSeat != null)
            {
                this.Title = ContextSeat.SeatId + "'s Info";
            }
            else
            {
                Logger logger = LogManager.GetLogger("EmptySeatView.Error");
                logger.Warn("Seat was passed in null. Unable to load seat info.");
                this.Title = "Unable to load context.";
            }
            
        }

        // If a professor has been selected (the professor combobox selection changed event fired) this method is called
        private void updateExamStatus()
        {
            // Setting up a little fail-safe because I noticed that the selection changed event fires but not selection is made
            // May need to test further
            this.IsExamCmbEnabled = this.SelectedProfessor >= 0;
            
            if (this.IsExamCmbEnabled)
            {
                try
                {
                    // Find the exams that were submitted by this professor and associate it with the exams combo box
                    this.ProfessorExams = this._seatingContext?.Exams?.Where((exam) => exam.Instructor == this.Professors[this.SelectedProfessor]).ToList();

                    // In order to get the default chosen (first) exam's information into the remaining fields, explicitly update the 
                    //  selected index to 0. Otherwise, you have to explicitly change the exam and then change back to select the first exam
                    this.SelectedIndex = 0;
                }
                catch (ArgumentException ae)
                {
                    Logger logger = LogManager.GetLogger("EmptySeatView.Error");
                    logger.Error(ae, "Linq error building Professor exam lists.");
                }
            }
        }

        private DelegateCommand seatExam = null;
        public DelegateCommand SeatExam =>
            seatExam ?? (seatExam = new DelegateCommand(SeatNewExam));
        private void SeatNewExam()
        {
            // Add the exam to our context seat
            // The mess below is a quick/dirty way to try and decouple the Seat exam copy from the exams read in through the 
            //  exam store. Ocassionally causes the seated exam view to crash after exam update because of missing
            //  professor information.
            this.ContextSeat.Exam = SelectedExam;

            // this.TimeIn is useful for tying to the textbox that displays the information
            this.ContextSeat.TimeIn = DateTime.Now.ToString();

            this.ContextSeat.Status = STATUS.Reserved;

            // In order to guarantee the information is saved, without also needing to inject the contexts into this view model,
            //  close the page after seating the exam?
            CloseDialog(SAVE_CHANGES);
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
