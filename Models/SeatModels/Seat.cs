using StudentSeating.Models.ApptModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;

namespace StudentSeating.Models
{
    [Serializable]
    public class Seat : IEquatable<Seat>, IComparable<Seat>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private int _id;
        /// <summary>
        /// The underlying database entity ID.
        /// </summary>
        public int Id
        {
            get
            {
                return _id;
            }

            set
            {
                this._id = value;
                NotifyPropertyChanged();
            }
        }

        private string seatId;

        /// <summary>
        /// The designation/"id" of the seat. A1, B3, C5, etc...
        /// </summary>
        public string SeatId
        {
            get
            {
                return seatId;
            }

            set
            {
                this.seatId = value;
                NotifyPropertyChanged();
            }
        }

        public string Type
        {
            get; set;
        }

        private STATUS seatStatus;
        public STATUS Status
        {
            get
            {
                return seatStatus;
            }

            set
            {
                this.seatStatus = value;

                NotifyPropertyChanged();
                NotifyPropertyChanged("StatusStyle");
            }
        }

        public STATUS DefaultStatus
        {
            get; set;
        }
        
        private string timeIn;
        public string TimeIn
        {
            get
            {
                return timeIn;
            }
            set
            {
                this.timeIn = value;

                NotifyPropertyChanged();
            }
        }

        [NotMapped]
        public SECTION Section
        {
            get
            {
                return (SECTION)this.Z;
            }
        }

        public int X
        {
            get;
            set;
        }

        public int Y
        {
            get;
            set;
        }

        public int Z
        {
            get;
            set;
        }

        private string name;
        public string StudentName
        {
            get
            {
                return name;
            }
            set
            {
                this.name = value;
                NotifyPropertyChanged();
            }
        }

        private string vid;
        public string StudentVid
        {
            get
            {
                return vid;
            }

            set
            {
                this.vid = value;
                NotifyPropertyChanged();
            }
        }

        private int? examId;
        public int? ExamId
        {
            get { return this.examId; }
            set { this.examId = value; }
        }

        private Exam exam;
        [NotMapped]
        public Exam Exam
        {
            get
            {
                return this.exam;
            }
            set
            {
                this.exam = value;
                NotifyPropertyChanged();
            }
        }

        public int CompareTo([AllowNull] Seat other)
        {
            if (this.Equals(other))
            {
                return 0;
            }

            //Sort nulls towards the end of the list, if present.
            if (null == other)
            {
                return -1;
            }

            if (this.Section != other.Section)
            {
                return this.Section.CompareTo(other.Section);
            }

            if (this.X != other.X)
            {
                return this.X.CompareTo(other.X);
            }

            if (this.Y != other.Y)
            {
                return this.Y.CompareTo(other.Y);
            }

            return this.SeatId.CompareTo(other.SeatId);
        }

        public bool Equals([AllowNull] Seat other)
        {
            if (null == other)
            {
                return false;
            }

            bool result;

            result = this.Section == other.Section;
            result &= this.SeatId == other.SeatId;
            result &= this.X == other.X;
            result &= this.Y == other.Y;
            result &= this.Z == other.Z;


            return result;
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return this.SeatId;
        }
    }

    public enum SECTION
    {
        OVERFLOW = 0,
        MAIN = 1,
        OSD = 2
    }

    public enum STATUS
    {
        Reserved = 0,
        Open = 1,
        OSD = 2,
        Broken = 3,
        Overflow = 4,
        Closed = 5
    }
}
