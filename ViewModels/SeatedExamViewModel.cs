using Microsoft.EntityFrameworkCore;
using NLog;
using Prism.Commands;
using Prism.Services.Dialogs;
using StudentSeating.Data;
using StudentSeating.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace StudentSeating.ViewModels
{
    class SeatedExamViewModel : IDialogAware, INotifyPropertyChanged
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

        private static string DEFAULT = "None";
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

        public string Password
        {
            get
            {
                return DEFAULT;
            }
        }

        private bool isExamCmbEnabled = false;
        public bool IsExamCmbEnabled
        {
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
            get
            {
                return this.selectedIndex;
            }
            set
            {
                selectedIndex = value;
                
                NotifyPropertyChanged();
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
                NotifyPropertyChanged();
                NotifyPropertyChanged("OtherItems");
                NotifyPropertyChanged("NotesFormulas");
                NotifyPropertyChanged("CalculatorsAllowed");
                NotifyPropertyChanged("Password");
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
                    Logger logger = LogManager.GetLogger("SeatedExamView.Error");
                    logger.Warn("DateTime {0} unsuccessfully parsed.", (null != value) ? value.ToString() : "");
                }
            }
        }

        private int selectedProfessor = 0;
        public int SelectedProfessor
        {
            get { return selectedProfessor; }
            set
            {
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

        private SeatingContext context;

        public SeatedExamViewModel(SeatingContext _context) : base()
        {
            this.context = _context;
        }

        public event Action<IDialogResult> RequestClose;

        private DelegateCommand<string> _closeDialogCommand;
        public DelegateCommand<string> DialogClose =>
            _closeDialogCommand ?? (_closeDialogCommand = new DelegateCommand<string>(CloseDialog));
        private void CloseDialog(string save)
        {
            //Object can be used to control how we interpret the closing of the window.
            ButtonResult result = ButtonResult.Cancel;
            DialogParameters parameters = new DialogParameters();

            // The cancel/close options all have command parameter "false". If SeatExam() calls this CloseDialog, it can pass
            //  in "save" to tell us we want to save.
            if ("save" == save)
            {
                result = ButtonResult.OK;
                parameters.Add("UpdatedSeat", ContextSeat);
            }

            //For instance, I may be able to differentiate between Cancel and 'Save' Button presses.
            RaiseRequestClose(new DialogResult(result, parameters));
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
            // In the ViewSeatInfo method of SeatingMainViewModel, passed three parameters
            // Take them, and stash them away
            parameters.TryGetValue("Seat", out Seat seat);
            parameters.TryGetValue("Context", out this._seatingContext);

            this.ContextSeat = seat;
            if (this.ContextSeat != null)
            {
                this.Title = ContextSeat.SeatId + "'s Info";
            }
            else
            {
                Logger logger = LogManager.GetLogger("SeatedExamView.Error");
                logger.Warn("Seat was passed in null. Unable to load seat info.");
                this.Title = "Unable to load seat info.";
            }

            try
            {
                this.Professors = this._seatingContext?.Exams.Select(e => e.Instructor).Distinct().ToList();
                this.SelectedExam = contextSeat.Exam;

                this.SelectedProfessor = this.Professors.FindIndex((s) => s.Equals(contextSeat.Exam.Instructor));
                this.ProfessorExams = this.context.Exams.Where((exam) => exam.Instructor == this.Professors[this.SelectedProfessor]).ToList();

                this.SelectedIndex = this.ProfessorExams.FindIndex((exam) => exam.Equals(this.SelectedExam));
                this.TimeIn = contextSeat.TimeIn;
                this.IsProfessorCmbEnabled = false;
            }
            catch (ArgumentException ae)
            {
                Logger logger = LogManager.GetLogger("SeatedExamView.Error");
                logger.Error(ae, "Linq error building Professor and Exam lists.");
            }
        }

        // If a professor has been selected (the professor combobox selection changed event fired) this method is called
        private void updateExamStatus()
        {
            // Setting up a little fail-safe because I noticed that the selection changed event fires but no selection is made
            // May need to test further
            this.IsExamCmbEnabled = (this.SelectedProfessor >= 0);

            if (this.IsExamCmbEnabled)
            {
                try
                {
                    // Find the exams that were submitted by this professor
                    List<Exam> temp = this.context.Exams.Where((exam) => exam.Instructor == this.Professors[this.SelectedProfessor]).ToList();
                    this.ProfessorExams = temp;
                }
                catch (ArgumentException ae)
                {
                    Logger logger = LogManager.GetLogger("SeatedExamView.Error");
                    logger.Error(ae, "Linq issue building exam list in exam update.");
                }
            }
        }

        private DelegateCommand endExam = null;
        public DelegateCommand ExamEnd =>
            endExam ?? (endExam = new DelegateCommand(EndExam));
        private void EndExam()
        {
            // Log that the test was ended as the student left
            Logger logger = LogManager.GetLogger("Seating");
            logger.Info("Student {0} {1} has been checked out.", this.ContextSeat.StudentVid, this.ContextSeat.StudentName);

            CloseExam();
        }

        private DelegateCommand clearExam = null;
        public DelegateCommand ExamClear =>
            clearExam ?? (clearExam = new DelegateCommand(ClearExam));
        private void ClearExam()
        {
            // Log that the test was cleared after the student left
            Logger logger = LogManager.GetLogger("Seating");
            logger.Info("Seat for student {0} {1} has been cleared.", this.ContextSeat.StudentVid, this.ContextSeat.StudentName);

            CloseExam();
        }

        private void CloseExam()
        {
            // Clear out the exam information
            this.ContextSeat.Exam = null;
            this.ContextSeat.ExamId = null;

            // Return the seat to its default status or closed for cleaning
            // TODO: Implement the Options storage, and tie to this condition
            if (true)
            {
                // Eventually, the condition will be plague-related.
                this.ContextSeat.Status = STATUS.Closed;
            }
            else
            {
                this.ContextSeat.Status = this.ContextSeat.DefaultStatus;
            }


            // Clear out the student's information
            this.ContextSeat.StudentName = "";
            this.ContextSeat.StudentVid = "";

            // Remove the timein stamp.
            this.ContextSeat.TimeIn = "";

            try
            {
                this._seatingContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException duce)
            {
                Logger logger = LogManager.GetLogger("SeatedExamView.Error");
                logger.Error(duce, "Concurrency issue updating Seats or Section table of the Seating database.");
            }
            catch (DbUpdateException due)
            {
                Logger logger = LogManager.GetLogger("SeatedExamView.Error");
                logger.Error(due, "Unable to update tables Seating or Sections of the Seating database.");
            }
            catch (Exception e)
            {
                Logger logger = LogManager.GetLogger("SeatedExamView.Error");
                logger.Error(e, "Error, see stacktrace for more information.");
            }
            // After the exam is closed out, close the dialog.
            CloseDialog(SAVE_CHANGES);
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
