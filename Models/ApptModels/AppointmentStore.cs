using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Prism.Events;
using System;
using System.Diagnostics;
using Exception = System.Exception;

using StudentSeating.Models.ApptModels;
using StudentSeating.Interfaces;

namespace StudentSeating.Models
{
    public class AppointmentStore : IAppointmentStore
    {
        private int apptRefreshRate = 60000;//3600000;

        public int AppointmentRefreshRate
        {
            get
            {
                return apptRefreshRate / 60000;//3600000;
            }
            set
            {
                this.apptRefreshRate = value * 60000;//3600000;
            }
        }

        public CancellationToken Cancel
        {
            get;
            set;
        }

        private IEventAggregator _event;
        public AppointmentStore(IEventAggregator eventAggregator)
        {
            _event = eventAggregator;
        }

        private AppointmentReader _reader;
        public void StartAppointmentUpdate(System.Action callback = null)
        {
            this.Cancel = new CancellationToken(false);
            _reader = new AppointmentReader();
            
            Task monitorAppts = new Task((async () =>
            {
                //Initial load upon open of program
                
                while (!this.Cancel.IsCancellationRequested)
                {
                    try
                    {
                        List<Appointment> appts = _reader.UpdateAppointments();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        if (null != appts)
                        {
                            this._event.GetEvent<IAppointmentStore.AppointmentsUpdated>().Publish(appts);
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine("AppointmentStore Error:\n" + e.StackTrace + "\n\t" + e.Message);
                    }

                    //Wait about an hour by default. Ideally, appointments need to be made 
                    // a day in advance. However, they almost never actually do...
                    // Plus, under plague-mode appointments may be made with much less lead-time.
                    await Task.Delay(this.apptRefreshRate);
                } 
            }), this.Cancel, TaskCreationOptions.LongRunning);

            monitorAppts.Start();
        }
    }
}
