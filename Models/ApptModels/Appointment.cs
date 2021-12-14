using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace StudentSeating.Models.ApptModels
{
    public class Appointment : IEquatable<Appointment>, IComparable<Appointment>
    {
        public int AppointmentId { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool OSD { get; set; }
        public string StudentName { get; set; }
        public string SeatId { get; set; }

        public int CompareTo([AllowNull] Appointment other)
        {
            //Intent is to bubble nulls to the end of the list, if present.
            if (other == null) return 1;
            if (this.Equals(other)) return 0;

            return this.Start.CompareTo(other.Start);
        }

        public bool Equals([AllowNull] Appointment other)
        {
            if (other == null)
            {
                return false;
            }

            bool result = true;

            result &= this.SeatId.Equals(other.SeatId);
            result &= this.StudentName.Equals(other.StudentName);
            result &= this.End.Equals(other.End);
            result &= this.Start.Equals(other.Start);
            result &= (this.OSD == other.OSD);

            return result;
        }

        public override string ToString()
        {
            return StudentName + "\nStart: " + Start + "\tEnd: " + End + "\n" +
                   "OSD Reserved: " + (OSD ? "Yes" : "No");
        }
    }
}
