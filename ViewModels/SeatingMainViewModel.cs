using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

using StudentSeating.Models;
using StudentSeating.Objects;
using StudentSeating.Interfaces;
using StudentSeating.Data;
using System.Threading;
using System.Linq;
using NLog;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;

namespace StudentSeating.ViewModels
{
    public class SeatingMainViewModel : BindableBase, INotifyPropertyChanged
    {
        public new event PropertyChangedEventHandler PropertyChanged;
        private IOptionStore _optionRepository = null;
        private IDialogService _dialogService = null;

        public SeatingContext Context
        {
            get; set;
        }

        public CancellationTokenSource Cancellation;

        public SeatingMainViewModel(IOptionStore options, SeatingContext _context, IDialogService dialogService, CancellationTokenSource _cancel)
        {
            // Context is the primary data context that is common throughout the program.
            // Contains seats, students and exam information.
            this.Context = _context;
            // This will eventually be replaced with an options read
            // TODO: Remove
            this.Context.Distancing = true;

            this.Cancellation = _cancel;

            this._optionRepository = options;
            this._dialogService = dialogService;

            if (this.Context.Sections.Count() <= 0)
            {
                initialSeatLoad();
            }

            // Note if I ever go back to eventAggregator patterns.
            //eventAggregator.GetEvent<IAppointmentStore.AppointmentsUpdated>().Subscribe((appts) =>
            //{
            //    this.SeatStructure.UpdateAppointments(appts);
            //});
        }

        private void initialSeatLoad()
        {
            try
            {
                string json = File.ReadAllText("Seating.json");
                if (!json.Equals(String.Empty))
                {
                    var fullJson = JObject.Parse(json);

                    List<string> sections = new List<string>();

                    foreach (var s in fullJson["sections"])
                    {
                        this.Context.Sections.Add(s.ToObject<Section>());
                    }

                    foreach (var seatInfo in fullJson["seats"])
                    {
                        this.Context.Seats.Add(seatInfo.ToObject<Seat>());

                    }

                    this.Context.SaveChangesAsync();
                }
            }
            catch (FileNotFoundException fnfe)
            {
                Logger logger = LogManager.GetLogger("SeatingMainModel.Error");
                logger.Error(fnfe, "Unable to find Seating.json.");
            }
            catch (JsonReaderException jre)
            {
                Logger logger = LogManager.GetLogger("SeatingMainModel.Error");
                logger.Error(jre, "Unable to parse Seating.json. Make sure there are not syntax issues.");
            }
            catch (DbUpdateConcurrencyException duce)
            {
                Logger logger = LogManager.GetLogger("SeatingMainModel.Error");
                logger.Error(duce, "Concurrency issue updating Seats or Section table of the Seating database.");
            }
            catch (DbUpdateException due)
            {
                Logger logger = LogManager.GetLogger("SeatingMainModel.Error");
                logger.Error(due, "Unable to update tables Seating or Sections of the Seating database.");
            }
            catch (Exception e)
            {
                Logger logger = LogManager.GetLogger("SeatingMainModel.Error");
                logger.Error(e, "Error, see stacktrace for more information.");
            }
            
        }

        private DelegateCommand closeCalled = null;
        public DelegateCommand CloseCalled =>
            closeCalled ?? (closeCalled = new DelegateCommand(HandleClose));
        private void HandleClose()
        {
            /*
            await this._examRepository.Close();
            await this._seatingRepository.Close();
            await this._optionRepository.Close();
            */
            try
            {
                Cancellation.Cancel();
            }
            catch (Exception e)
            {
                Logger logger = LogManager.GetLogger("SeatingMainModel.Error");
                logger.Error(e, "Issue with canceling execution through the cancellation token.");
            }
        }

        private DelegateCommand refreshData = null;
        public DelegateCommand RefreshData =>
            refreshData ?? (refreshData = new DelegateCommand(CommandRefreshData));

        private void CommandRefreshData()
        {
            
        }

        private DelegateCommand applyOptionsUpdate = null;
        public DelegateCommand OptionsUpdate =>
            applyOptionsUpdate ?? (applyOptionsUpdate = new DelegateCommand(ApplyOptionsUpdate));
        private void ApplyOptionsUpdate()
        {

        }

        private DelegateCommand revertOptionsUpdate = null;
        public DelegateCommand OptionsRevert =>
            revertOptionsUpdate ?? (revertOptionsUpdate = new DelegateCommand(RevertOptionsUpdate));
        private void RevertOptionsUpdate()
        {

        }

        private DelegateCommand openSeatingLogs = null;
        public DelegateCommand OpenSeatingLogs =>
            openSeatingLogs ?? (openSeatingLogs = new DelegateCommand(CommandSeatingLogs));

        private void CommandSeatingLogs()
        {
            
        }

        private DelegateCommand examFilePath = null;
        public DelegateCommand SelectExamFilePath =>
            examFilePath ?? (examFilePath = new DelegateCommand(SelectExamPath));

        private void SelectExamPath()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            bool? result = ofd.ShowDialog();

            if (result.HasValue && result.Value)
            {
                //this.ExamFilePath = ofd.FileName;
            }
        }

        private DelegateCommand<SeatButton> viewSeatInfo = null;
        public DelegateCommand<SeatButton> ViewSeatInfo =>
            viewSeatInfo ?? (viewSeatInfo = new DelegateCommand<SeatButton>(ViewSeat));
        private void ViewSeat(SeatButton source)
        {
            Seat seat = source.SeatInfo;
            
            if (seat != null)
            {
                // For the dialog service below, the only difference between bringing up the seated dialog window and the empty dialog
                //  window is whether an exam is in the context seat. And since the only difference is the string passed to the method,
                //  determine which to send here
                string destination = (null != seat.Exam) ? "SeatedExam" : "EmptySeat";
                DialogParameters parameters = new DialogParameters();
                parameters.Add("Seat", seat);
                parameters.Add("Context", this.Context);

                //using the dialog service as-is
                _dialogService.ShowDialog(destination, parameters, r =>{});
            }
        }

        private DelegateCommand seatExam = null;
        public DelegateCommand SeatExam =>
            seatExam ?? (seatExam = new DelegateCommand(FindSeat));
        private void FindSeat()
        {
            Boolean examSeated = false;
            Seat seat = null;
            DialogParameters parameters = new DialogParameters();
            parameters.Add("Context", this.Context);
            _dialogService.ShowDialog("FindSeat", parameters, r => { 
                examSeated = (r.Result == ButtonResult.OK);
                if (examSeated)
                {
                    r.Parameters.TryGetValue<Seat>("Seat", out Seat s);
                    seat = s;
                }
            });

            if (examSeated)
            {

                // Treat it as if we'd manually clicked on the seat, except we know the seat is currently open
                parameters = new DialogParameters();
                parameters.Add("Seat", seat);
                parameters.Add("Context", this.Context);

                // Since we know the seat is open, we can just open the EmptySeat with our desired parameters
                _dialogService.ShowDialog("SeatedExam", parameters, r => { });
            }
        }

        private DelegateCommand clearRooms = null;
        public DelegateCommand ClearRooms =>
            clearRooms ?? (clearRooms = new DelegateCommand(RoomClear));
        private void RoomClear()
        {
            foreach (Seat s in this.Context.Seats)
            {
                if (s.Status == STATUS.Open)
                    continue;
                s.Exam = null;
                s.StudentName = null;
                s.StudentVid = null;
                s.Status = s.DefaultStatus;
                s.TimeIn = null;
            }
            this.Context.SaveChangesAsync();
        }
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
