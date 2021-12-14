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
using System.Threading.Tasks;
using System.Windows;

namespace StudentSeating.ViewModels
{
    class FindSeatViewModel : IDialogAware, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // A default string to populate textboxes with, to differentiate "nothing was entered", "None was entered" and errors
        private static string DEFAULT = "None*";
        private static string SAVE = "SAVE";

        public Seat ContextSeat { get; set; }

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

        private DelegateCommand<string> _closeDialogCommand;
        public DelegateCommand<string> DialogClose =>
            _closeDialogCommand ?? (_closeDialogCommand = new DelegateCommand<string>(CloseDialog));

        private void CloseDialog(string result)
        {
            Boolean save = result.Equals(SAVE);
            ButtonResult res = (save ? ButtonResult.OK : ButtonResult.Cancel);
            DialogParameters parameters = null;
            if (save)
            {
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

                parameters = new DialogParameters();
                parameters.Add("Seat", this.ContextSeat);
            }
            
            RaiseRequestClose(new DialogResult(res, parameters));
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
            parameters.TryGetValue("Context", out this._seatingContext);

            this.Title = "Find a Seat";
            this.ContextSeat = null;

            try
            {
                this.Professors = this._seatingContext?.Exams.Select(e => e.Instructor).Distinct().ToList();
            }
            catch (ArgumentException ae)
            {
                Logger logger = LogManager.GetLogger("SeatedExamView.Error");
                logger.Error(ae, "Linq error building Professor and Exam lists.");
            }
        }

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
                    IQueryable<Exam> t = this._seatingContext?.Exams?.Where(exam => exam.Instructor == this.Professors[this.SelectedProfessor]);

                    this.ProfessorExams = t.Distinct().ToList();
                    //this.ProfessorExams = this._seatingContext?.Exams?.Where((exam) => exam.Instructor == this.Professors[this.SelectedProfessor]).Distinct().ToList();
                    this.ProfessorExams.Sort((t,o) => t.Instructor.CompareTo(o.Instructor));
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

        private DelegateCommand findSeat = null;
        public DelegateCommand SeatFind =>
            findSeat ?? (findSeat = new DelegateCommand(FindSeat));
        public async void FindSeat()
        {
            if (isInvalidSeating())
            {
                // The user hasn't entered something necessary, don't bother finding a seat.
                return;
            }
            // Check for seat availability
            // The seat with the best availability will be stored in ContextSeat.

            Boolean plague = this._seatingContext.Distancing; // Will read in from some options storage

            List<Task<Tuple<Seat, bool>>> tasks = (from Seat s in this._seatingContext.Seats select CheckAvailability(s, SelectedExam, plague)).ToList();
            List<Seat> available = new List<Seat>();
            List<Seat> unavailable = new List<Seat>();

            while (tasks.Any())
            {
                Task<Tuple<Seat, bool>> temp = await Task.WhenAny(tasks);

                tasks.Remove(temp);
                Tuple<Seat, bool> tuple = temp.Result;

                if (null == tuple)
                {
                    continue;
                }

                // Segmenting between available/unavailable seats
                if (tuple.Item2)
                {
                    unavailable.Add(tuple.Item1);
                }
                else
                {
                    available.Add(tuple.Item1);
                }
            }

            List<Task<IEnumerable<Seat>>> removed = (from Seat s in unavailable select FindInRadius(s, available, plague)).ToList();
            //HashSet<Seat> set = new HashSet<Seat>();

            while (removed.Any())
            {
                Task<IEnumerable<Seat>> temp = await Task.WhenAny(removed);

                removed.Remove(temp);
                IEnumerable<Seat> toBeRemoved = temp.Result;

                if (null == toBeRemoved)
                {
                    continue;
                }

                foreach (Seat s in toBeRemoved)
                {
                    available.Remove(s);
                }
            }

            if (available.Count() >= 2)
                available.Sort((t, o) => t.SeatId.CompareTo(o.SeatId));

            if (available.Count() > 0)
            {
                this.ContextSeat = available[0];

                this.ContextSeat.Exam = this.SelectedExam;
                this.ContextSeat.StudentName = this.StudentName;
                this.ContextSeat.StudentVid = this.StudentVID;

                this.ContextSeat.TimeIn = DateTime.Now.ToString();

                this.ContextSeat.Status = STATUS.Reserved;


                CloseDialog(SAVE);
            }
            else
            {
                MessageBox.Show("Unable to place student.\n" +
                    "Check for empty seats, and possible manually place.\n", 
                    "No Seats Found", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        public static Task<Tuple<Seat, bool>> CheckAvailability(Seat seat, Exam selected, bool plague)
        {
            Tuple<Seat, bool> temp = null;
            if (null == seat)
            {
                temp = null;
            }
            else if (null == seat.Exam && seat.Status == STATUS.Open && seat.Type.Equals(selected.ExamType) && seat.Section != SECTION.OSD)
            {
                temp = new Tuple<Seat, bool>(seat, false);
            }
            else if (selected.FuzzyEquals(seat.Exam))
            {
                temp = new Tuple<Seat, bool>(seat, true);
            }

            return Task.FromResult(temp);
        }

        public static Task<IEnumerable<Seat>> FindInRadius(Seat closed, IEnumerable<Seat> available, bool plague, int radius = 1)
        {
            IEnumerable<Seat> inRadius = null;

            radius += (plague ? 1 : 0);

            int leftBound = closed.X - radius;
            int rightBound = closed.X + radius;
            int upperBound = closed.Y + radius;
            int lowerBound = closed.Y - radius;

            // Don't include the seat itself, and don't bother looking at seats in other rooms or seats without exams.
            // Find seats in a larger square area around the center.
            inRadius = available.Where(s => closed != s && s.Z == closed.Z &&
                                            s.X >= leftBound && s.X <= rightBound &&
                                            s.Y >= lowerBound && s.Y <= upperBound);

            // If we want a more elliptical, social-distancing radius
            if (plague)
            {
                int rSquared = radius * radius;
                int rowFactor = 4;
                // Weed out the ones outside of that radius
                inRadius = inRadius.Where(s => ((s.X - closed.X) * (s.X - closed.X) / rowFactor + (s.Y - closed.Y) * (s.Y - closed.Y) * rowFactor) <= rSquared);
            }

            return Task.FromResult(inRadius);
        }

        public bool isInvalidSeating()
        {
            bool result = false;

            // The seating is invalid if
            //  There is no exam selected
            result |= (null == this.SelectedExam);
            //  There is no student name/VID entered.
            //  If either is entered, it'll be false and we have valid seating
            result |= (null == this.StudentName || this.StudentName.Equals(String.Empty));
            result |= (null == this.StudentVID || this.StudentVID.Equals(String.Empty));

            return result;
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
