using Prism.Events;
using StudentSeating.Models.ApptModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace StudentSeating.Interfaces
{
    public interface IAppointmentStore
    {
        public class AppointmentsUpdated : PubSubEvent<List<Appointment>> { }

        public int AppointmentRefreshRate { get; set; }

        public void StartAppointmentUpdate(Action callback = null);

    }
}
