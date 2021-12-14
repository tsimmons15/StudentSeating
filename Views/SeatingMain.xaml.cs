using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using StudentSeating.Interfaces;
using StudentSeating.Models;
using StudentSeating.Objects;
using StudentSeating.ViewModels;
using Prism.Events;
using StudentSeating.Data;
using System.Threading;
using System.Linq;

namespace StudentSeating.Views
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SeatingMain : MetroWindow
    {
        public SeatingMain(ExamExcelReader reader)
        {
            InitializeComponent();

            _ = reader.HandleUpdate();

            // TODO: Set up event listeners to capture scroll in the windows themselves?

            Grid seating = SeatingFragment;

            // It looks like part of InitializeComponent() sets up the view model before continuing, so vm should be mostly set up
            SeatingMainViewModel vm = (SeatingMainViewModel)this.DataContext;
            List<Seat> seats = vm.Context.Seats.ToList();
            List<Section> sections = vm.Context.Sections.ToList();

            // Explicit set up of the 1 column that will contain all the seating information
            // Grids seem to be the easiest way to allow for autosizing content. So, even though we don't want a "grid", we're using one
            seating.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            int i = 0;

            List<Grid> seatGrids = new List<Grid>();
            foreach (Section s in sections)
            {
                // Add a new row to the outer seating grid, to accomodate to new section
                seating.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

                // Set up the expander to allow us to hide a section.
                Expander exp = new Expander();
                exp.Header = "Section: " + s.Title;
                exp.ExpandDirection = ExpandDirection.Down;

                // The grid that will hold the seats.
                Grid g = new Grid();

                g.HorizontalAlignment = HorizontalAlignment.Left;

                // Based on this sections info, set up the inner grid with the correct number of rows and columns
                for (int row = 0; row < s.RowCount; row++)
                {
                    g.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
                }

                for (int col = 0; col < s.MaxRow; col++)
                {
                    g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                }

                // Scroll bar allows us to scroll if needed.
                ScrollViewer scroll = new ScrollViewer();
                scroll.MinHeight = 150;
                scroll.CanContentScroll = true;
                scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

                scroll.Content = g;

                // We haven't added the actual seats yet, so store the grids for future reference
                seatGrids.Add(g);

                exp.Content = scroll;

                exp.IsExpanded = true; // Need to figure out a way to get these collapsed by default, but not taking up expanded size...

                // The order of i will be determined by the index in seating.json
                Grid.SetRow(exp, i);
                Grid.SetColumn(exp, 0);

                seating.Children.Add(exp);

                i++;
            }

            // Then add the individual seats to our g's.
            foreach (Seat seat in vm.Context.Seats.ToList())
            {
                // Create the button that encapsulates our seat info, assigning it the information
                // Simplifies the click event handler since the "button" has all the seat info already.
                SeatButton sb = new SeatButton();
                sb.SeatInfo = seat;

                sb.Margin = new Thickness(1, 5, 1, 0);

                // I don't want the buttons too big, but also want them to be mostly legible. Plans are currently to 
                //  set up some kind of config file, this info may show up in there eventually.
                sb.MinHeight = 35;
                sb.MaxHeight = 70;
                sb.MinWidth = 90;
                sb.MaxWidth = 180;

                // Assign the seats to their respective cells
                Grid.SetRow(sb, seat.Y);
                Grid.SetColumn(sb, seat.X);

                // Wire the command and parameter so the "click event" brings up our seat dialog.
                sb.Command = vm.ViewSeatInfo;
                sb.CommandParameter = sb;

                sb.Style = (Style)FindResource("SeatStyle");

                sb.Status = sb.SeatInfo.Status.ToString();

                seatGrids[seat.Z].Children.Add(sb);
            }

            // Signal that the layout may have changed and needs to be updated
            this.UpdateLayout();
            //Clunky workaround since the Expander starts off taking the full 200px width on start, but by default adjusts
            // its container width when it expands/collapses. This, and the IsEnabled in the XAML forces it to load 'properly'.
            optionsExpander.IsExpanded = false;
        }
    }
}
