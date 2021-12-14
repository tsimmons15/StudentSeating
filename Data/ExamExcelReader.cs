using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;

using StudentSeating.Models;
using System.IO;
using System.Text.Json;
using System.Diagnostics;

namespace StudentSeating.Data
{
    public class ExamExcelReader
    {
        public SeatingContext Context { get; set; }

        private string _filePath = @"C:\Users\Thrym\Desktop\Testing Center Log - Current.xlsx";
        public string ExcelFilePath
        {
            get { return _filePath; }
            set { _filePath = value; }
        }

        private int _examUpdateRate = 120000;
        /// <summary>
        /// The delay between refreshing exams, measured in whole minutes.
        /// </summary>
        public int ExamUpdateRate
        {
            get
            {
                return _examUpdateRate;
            }
            set
            {
                this._examUpdateRate = value * 60000;
            }
        }
        public CancellationToken Cancel { get; set; }

        public ExamExcelReader(SeatingContext _context, CancellationTokenSource _cancel)
        {
            this.Context = _context;
            this.Cancel = _cancel.Token;
        }

        public Task<List<Exam>> UpdateFromExcel()
        {
            Excel.Application excel = new Excel.Application();
            Excel.Workbooks workbooks = excel.Workbooks;
            Excel.Workbook testLog = workbooks.Open(ExcelFilePath, ReadOnly: true);
            Excel.Sheets semesters = testLog.Worksheets;
            Excel.Worksheet currSem = (Excel.Worksheet)semesters[semesters.Count];
            
            const int CRN = 2, Course = 3, ExamName = 4, Instructor = 5, DateReceived = 7, StartDate = 9, DeadlineDate = 11, Calculators = 13, NotesFormulas = 14, OtherItems = 15, ExamType = 18;
            Excel.Range usedRange = currSem.UsedRange;
            object[,] log = (usedRange.Value as object[,]);

            List<Exam> exams = new List<Exam>();

            for(int i = 2; i <= log.GetLength(0) && !Cancel.IsCancellationRequested; i++)
            {
                //Beyond data entry errors, sometimes professors do not actually give us all the data we need.
                // The ternaries are to capture those issues.
                var crn = (null == log[i, CRN]) ? "" : log[i, CRN];
                var course = (null == log[i, Course]) ? "" : log[i, Course];
                var examName = (null == log[i, ExamName]) ? "" : log[i, ExamName];
                var instructor = (null == log[i, Instructor]) ? "" : log[i, Instructor];
                var dateReceived = (null == log[i, DateReceived]) ? "" : log[i, DateReceived];
                var startDate = (null == log[i, StartDate]) ? "" : log[i, StartDate];
                var deadlineDate = (null == log[i, DeadlineDate]) ? "" : log[i, DeadlineDate];
                var calculators = (null == log[i, Calculators]) ? "" : log[i, Calculators];
                var notesFormulas = (null == log[i, NotesFormulas]) ? "" : log[i, NotesFormulas];
                var otherItems = (null == log[i, OtherItems]) ? "" : log[i, OtherItems];
                var examType = (null == log[i, ExamType]) ? "" : log[i, ExamType];

                DateTime.TryParse(dateReceived.ToString(), out DateTime date);
                dateReceived = date;
                DateTime.TryParse(startDate.ToString(), out date);
                startDate = date;
                DateTime.TryParse(deadlineDate.ToString(), out date);
                deadlineDate = date;


                Exam exam = new Exam()
                {
                    CRN = crn.ToString(),
                    Course = course.ToString(),
                    ExamName = examName.ToString(),
                    Instructor = instructor.ToString(),
                    DateReceived = (DateTime)dateReceived,
                    StartDate = (DateTime)startDate,
                    DeadlineDate = (DateTime)deadlineDate,
                    CalculatorsAllowed = calculators.ToString(),
                    NotesFormulas = notesFormulas.ToString(),
                    OtherItems = otherItems.ToString(),
                    ExamType = GetExamType(examType.ToString()),
                    TimeRead = DateTime.Now
                };
                exams.Add(exam);
            }

            testLog.Close();
            workbooks.Close();
            excel.Quit();

            // If the cancel was called, no need to do anything else with the partial info we've collected. Just return.
            if (Cancel.IsCancellationRequested)
            {
                return Task.FromCanceled<List<Exam>>(Cancel);
            }

            return Task.FromResult(exams);
        }

        public async Task HandleUpdate()
        {
            while (!this.Cancel.IsCancellationRequested)
            {
                try
                {
                    // Get the total list of exams stored in the excel file, stored in ExamInfo
                    List<Exam> info = await UpdateFromExcel();

                    // If the Update returned the exam info
                    if (null != info)
                    {
                        List<Exam> dbExams = this.Context.Exams.ToList();

                        // The if(i < info.ExamList.Count) is to counter the possibility of somebody deleting rows.
                        // Because of the way everything is set up, I do not see a way to reliably tell whether an entry is changed vs. a new entry
                        // was inserted at any point of the list.
                        // For inserting rows, it just means there'll be a costly update. For deletion though, it can lead to argument out of bounds
                        // on the info.ExamList.

                        // Run through to edit any existing exams.
                        int i = 0;
                        for (i = 0; i < dbExams.Count; i++)
                        {
                            if (i >= info.Count)
                                break;

                            if (!dbExams[i].Equals(info[i]))
                            {
                                // Updating alone throws an error as due to an entity already being tracked with that Id.
                                // So, remove it and add the new version. Indexed on Id by default (**assumption**) so it should not cause problems.
                                info[i].Id = dbExams[i].Id;
                                this.Context.Exams.Remove(dbExams[i]);
                                this.Context.Exams.Add(info[i]);
                            }
                        }

                        // More likely than edits, there will be entirely new entities.
                        // Run through the left over exams on the tail end. They may have been included in the db before (an insertion happened),
                        // but due to the previous foreach, they were removed and so need to be added back.
                        for (; i < info.Count; i++)
                        {
                            this.Context.Exams.Add(info[i]);
                        }

                        // Delete exams?
                        // If i >= info.Count, number of exams read in from Excel < dbExams.
                        // i is the point which info runs out, other exams are not in the current Excel file, and need to be removed.
                        for (; i < dbExams.Count; i++)
                        {
                            this.Context.Exams.Remove(dbExams[i]);
                        }

                        await this.Context.SaveChangesAsync();
                    }
                }
                catch (ArgumentOutOfRangeException aoore)
                {
                    // Add logging, eventually
                }
                catch (Exception e)
                {
                    // Generic, catch-all
                }

                // Wait ~ 5 minutes, or however long, to check again. Probably will increase in the future...
                await Task.Delay(this.ExamUpdateRate);
            }
        }

        private static readonly string[] RETURN_METHOD =
        {
            "pick", "mail", "scan"
        };

        private static String GetExamType(string method)
        {
            string ret = "Computer";
            int dist = 0;

            // If the field equals either Paper or Computer, return it.
            if (method.Equals("Paper") || method.Equals("Computer"))
            {
                return method;
            }

            // If it's not equal to either, check for either being the closest match.
            // Assumption of Computer as a default is a decent conservative estimate.
            // Worst case, you can just turn off the computer
            dist = Exam.SpellingDistance(method, "Paper");
            if (Exam.SpellingDistance(method,"Computer") > dist)
            {
                ret = "Paper";
            }

            // Not really sure how to check for other possibilities, including wildly different entries due to quickly
            // entering the information without checking...
            return ret;
        }

        private static DateTime validateDate(dynamic date)
        {

            //A valid date in Excel's format is going to be stored as an integer, so an easy way
            // to validate the date is try to cast as Int, and if it fails, return DateTime.MinValue 
            // as a placehiolder.
            bool isValid = int.TryParse(date.ToString(), out int result);

            //Based on the TryParse above:
            // If the parse was successful, the result will contain the OADate formatted integer
            //  so we can convert the integer to a valid DateTime. 
            // Otherwise, we need to return the DateTime.MinValue as a placeholder, since the program
            //  does not do well with null DateTimes and a DateTime object needs to be a "valid" date.
            return (isValid ? DateTime.FromOADate(result) : DateTime.MinValue);
        }
    }
}
