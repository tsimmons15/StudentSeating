using StudentSeating.Models;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace StudentSeating.Objects
{
    public partial class SeatButton : Button
    {

        private Seat seatInfo;
        public Seat SeatInfo
        {
            get
            {
                return seatInfo;
            }
            set
            {
                seatInfo = value;
                if (null != seatInfo)
                {
                    this.displayContent.Text = this.SeatText;

                    seatInfo.PropertyChanged += StatusChangedHandler;
                }
            }
        }

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(
                nameof(Status), typeof(string),
                typeof(SeatButton), new PropertyMetadata("Open"));
        public String Status
        {
            get
            {
                return (string)GetValue(StatusProperty);
            }
            set
            {
                SetValue(StatusProperty, value);
            }
        }

        public String SeatText
        {
            get
            {
                string ret = this.SeatInfo.SeatId;
                string name = this.SeatInfo.StudentName;
                string vid = this.SeatInfo.StudentVid;
                if (null != name && !name.Equals(String.Empty))
                {
                    ret += "\n" + name;
                }
                else if (null != vid && !vid.Equals(String.Empty))
                {
                    ret += "\n" + vid;
                }

                return ret;
            }
        }

        // A very clunky way of getting the StatusProperty tied to the Seat's Status...
        private void StatusChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Status"))
            {
                this.Status = (sender as Seat).Status.ToString();
            }
            else if (e.PropertyName.Equals("StudentName") || e.PropertyName.Equals("StudentVid"))
            {
                this.displayContent.Text = this.SeatText;
            }
        }

        private TextBlock displayContent;

        public SeatButton() : base()
        {
            this.displayContent = new TextBlock();
            displayContent.TextAlignment = TextAlignment.Center;
            InitializeComponent();
            this.Content = displayContent;
        }

        public SeatButton(IContainer container) : base()
        {
            InitializeComponent();
        }
    }
}
