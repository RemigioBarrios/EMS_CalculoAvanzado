using System.Windows;
using CalculusApp.Models;

namespace CalculusApp.Dialogs
{
    public partial class HeadingDialog : Window
    {
        private readonly AssignmentHeading _heading;

        public HeadingDialog(AssignmentHeading heading)
        {
            InitializeComponent();
            _heading = heading;

            TitleBox.Text = _heading.Title;
            SubtitleBox.Text = _heading.Subtitle;
            TeacherBox.Text = _heading.Teacher;
            NameCheck.IsChecked = _heading.ShowName;
            DateCheck.IsChecked = _heading.ShowDate;
            PeriodCheck.IsChecked = _heading.ShowPeriod;
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            _heading.Title = TitleBox.Text.Trim();
            _heading.Subtitle = SubtitleBox.Text.Trim();
            _heading.Teacher = TeacherBox.Text.Trim();
            _heading.ShowName = NameCheck.IsChecked == true;
            _heading.ShowDate = DateCheck.IsChecked == true;
            _heading.ShowPeriod = PeriodCheck.IsChecked == true;

            DialogResult = true;
            Close();
        }
    }
}
