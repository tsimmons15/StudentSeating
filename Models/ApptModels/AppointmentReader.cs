using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Office.Interop.Outlook;

namespace StudentSeating.Models.ApptModels
{
    public class AppointmentReader
    {
        public List<Appointment> UpdateAppointments()
        {
            List<Appointment> appts = new List<Appointment>();

            Application outlook = new Application();
            NameSpace nameSpace = outlook.GetNamespace("MAPI");
            MAPIFolder calendar = nameSpace.GetDefaultFolder(OlDefaultFolders.olFolderCalendar);
            Items items = calendar.Items;
            
            DateTime currYear = DateTime.Now;
            currYear = new DateTime(currYear.Year, 8,19);

            List<Appointment> filtered = dateFilter(items);
            foreach (Appointment i in filtered)
            {
                Trace.WriteLine(i);
            }


            items = null;
            calendar = null;
            nameSpace = null;
            outlook.Quit();
            return appts;
        }

        private List<Appointment> dateFilter(Items items)
        {
            DateTime start = DateTime.Now;
            DateTime end = start.AddHours(1);

            List<Appointment> filtered = new List<Appointment>();

            foreach (AppointmentItem i in items)
            {
                if (i.Start > start && i.End < end)
                {
                    //TODO: Finalize the parsing of the appointments.
                    filtered.Add(new Appointment()
                    {
                        End=i.End,
                        Start=i.Start,
                        StudentName=i.Subject,
                        SeatId="A05",
                        OSD=false
                    });
                }
            }

            return filtered;
        }
    }
}
