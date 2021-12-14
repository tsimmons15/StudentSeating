using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace StudentSeating.Models
{
    [Serializable]
    public class Exam : IEquatable<Exam>, INotifyPropertyChanged
    {
        private static readonly int threshold = 3;

        public event PropertyChangedEventHandler PropertyChanged;

        private int id;
        public int Id
        {
            get
            {
                return this.id;
            }
            set
            {
                this.id = value;
            }
        }

        private string crn;
        public string CRN
        {
            get
            {
                return crn;
            }
            set
            {
                this.crn = value;

                NotifyPropertyChanged();
            }
        }

        private string course;
        public string Course
        {
            get
            {
                return course;
            }
            set
            {
                this.course = value;

                NotifyPropertyChanged();
            }
        }

        private string examName;
        public string ExamName
        {
            get
            {
                return examName;
            }
            set
            {
                this.examName = value;

                NotifyPropertyChanged();
            }
        }

        private string instructor;

        public string Instructor
        {
            get
            {
                return instructor;
            }
            set
            {
                this.instructor = value;

                NotifyPropertyChanged();
            }
        }

        private DateTime dateReceived;
        public DateTime DateReceived
        {
            get
            {
                return dateReceived;
            }
            set
            {
                this.dateReceived = value;

                NotifyPropertyChanged();
            }
        }

        private DateTime startDate;
        public DateTime StartDate
        {
            get
            {
                return startDate;
            }
            set
            {
                this.startDate = value;

                NotifyPropertyChanged();
            }
        }

        private DateTime deadLineDate;
        public DateTime DeadlineDate
        {
            get
            {
                return deadLineDate;
            }
            set
            {
                this.deadLineDate = value;

                NotifyPropertyChanged();
            }
        }

        private string calculators;
        public string CalculatorsAllowed
        {
            get
            {
                return calculators;
            }
            set
            {
                this.calculators = value;

                NotifyPropertyChanged();
            }
        }

        private string notesFormulas;
        public string NotesFormulas
        {
            get
            {
                return notesFormulas;
            }
            set
            {
                this.notesFormulas = value;

                NotifyPropertyChanged();
            }
        }

        private string otherItems;
        public string OtherItems
        {
            get
            {
                return otherItems;
            }
            set
            {
                this.otherItems = value;

                NotifyPropertyChanged();
            }
        }

        private string examType;
        public string ExamType
        {
            get
            {
                return examType;
            }
            set
            {
                this.examType = value;

                NotifyPropertyChanged();
            }
        }

        private DateTime timeRead;
        public DateTime TimeRead
        {
            get { return this.timeRead; }
            set { this.timeRead = value; }
        }

        /// <summary>
        /// Create a "fuzzy" match between an exam based on the information given. Biggest offender is spelling mistakes
        /// and typos.
        /// FuzzyEquals is a conservative equality checker so, if in doubt, will return they are a match and to go look for 
        /// other available spots.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>True if there is enough evidence for the exams being similar, false otherwise.</returns>
        public bool FuzzyEquals(Exam other)
        {
            if (null == other)
            {
                return false;
            }
            
            // If there is strict equality (which is going to be faster), don't bother. 
            // Will slow down slightly discovering typos, which should be the minority of situations.
            if (this.PartialEquals(other))
			{
				return true;
			}

            int conflicts = 0;
            int dist = 0;

            dist = SpellingDistance(getProcessedString(other.CRN), getProcessedString(this.CRN));
            conflicts += (dist > Exam.threshold ? 1 : 0);
            dist = SpellingDistance(getProcessedString(other.Course), getProcessedString(this.Course));
            conflicts += (dist > Exam.threshold ? 1 : 0);
            dist = SpellingDistance(getProcessedString(other.ExamName), getProcessedString(this.ExamName));
            conflicts += (dist > Exam.threshold ? 1 : 0);
            dist = SpellingDistance(getProcessedString(other.Instructor), getProcessedString(this.Instructor));
            conflicts += (dist > Exam.threshold ? 1 : 0);

            // If conflicts is above the threshold, it's similar enough to be the same exam
            //  remembering that the FuzzyEquals wants to be conservative in favor of exams being equal.
            return conflicts > Exam.threshold;
        }

        /// <summary>
        /// A static version of FuzzyEquals that just takes two strings and compares them. Provides a more direct
        /// way to compare exam information without comparing ALL the information.
        /// </summary>
        /// <param name="original">The string containing information from the exam being seated.</param>
        /// <param name="other">The string containing information from the exam already seated.</param>
        /// <returns>True if the two strings are similar enough, false otherwise.</returns>
        public static bool FuzzyCheck(String original, string other)
        {
            return SpellingDistance(original, other) > Exam.threshold;
        }

        /// <summary>
        /// Used in the FuzzyEquals for exam/instructor similarity. Spelling mistakes are easy to make, looking at spelling 
        /// distance can possibly be used to estimate how likely the two are to be an approximate match.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static int SpellingDistance(string source, string target)
        {
            int[,] dist = new int[2, target.Length + 1];

            for (int i = 0; i <= target.Length; i++)
            {
                dist[0, i] = i;
            }

            for (int i = 0; i < source.Length; i++)
            {
                dist[(i + 1) % 2, 0] = i + 1;

                for (int j = 0; j < target.Length; j++)
                {
                    dist[(i + 1) % 2, j + 1] = Math.Min(Math.Min(dist[(i % 2), j + 1] + 1, dist[(i + 1) % 2, j] + 1),
                                            dist[(i % 2), j] + (source[i] == target[j] ? 0 : 1));
                }
            }

            return dist[source.Length%2, target.Length];
        }

        public bool Equals(Exam other)
        {
            bool result = true;

            if (null == other)
            {
                return false;
            }

            result &= getProcessedString(other.CRN).Equals(getProcessedString(this.CRN));
            result &= getProcessedString(other.Course).Equals(getProcessedString(this.Course));
            result &= getProcessedString(other.ExamName).Equals(getProcessedString(this.ExamName));
            result &= getProcessedString(other.Instructor).Equals(getProcessedString(this.Instructor));
            result &= getProcessedString(other.ExamType).Equals(getProcessedString(this.ExamType));
            result &= getProcessedString(other.DateReceived.ToString()).Equals(getProcessedString(this.DateReceived.ToString()));
            result &= getProcessedString(other.StartDate.ToString()).Equals(getProcessedString(this.StartDate.ToString()));
            result &= getProcessedString(other.DeadlineDate.ToString()).Equals(getProcessedString(this.DeadlineDate.ToString()));
            result &= getProcessedString(other.CalculatorsAllowed.ToString()).Equals(getProcessedString(this.CalculatorsAllowed.ToString()));
            result &= getProcessedString(other.NotesFormulas.ToString()).Equals(getProcessedString(this.NotesFormulas.ToString()));
            result &= getProcessedString(other.OtherItems.ToString()).Equals(getProcessedString(this.OtherItems.ToString()));

            return result;
        }
        
        public bool PartialEquals(Exam other)
        {
            bool result = true;

            if (null == other)
            {
                return false;
            }

            result &= getProcessedString(other.CRN).Equals(getProcessedString(this.CRN));
            result &= getProcessedString(other.Course).Equals(getProcessedString(this.Course));
            result &= getProcessedString(other.ExamName).Equals(getProcessedString(this.ExamName));
            result &= getProcessedString(other.Instructor).Equals(getProcessedString(this.Instructor));

            return result;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Exam);
        }

        public override int GetHashCode()
        {
            return (this.CRN, this.Course, this.ExamName).GetHashCode();
        }

        private string getProcessedString(string raw)
        {
            if (raw == null)
                return "";
            // Strip string of spaces, leading or internal, and lower-case. 
            return raw.Trim().Replace(" ", "").ToLower();
        }

        public override string ToString()
        {
            return this.Course + " " + this.ExamName;
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
