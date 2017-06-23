using FontAwesome.UWP;
using System.ComponentModel;
using Windows.UI.Xaml.Controls;

namespace FixedPivot
{
    public class PivotHeaderModel : INotifyPropertyChanged
    {
        private int _badge;

        public int Badge
        {
            get => _badge;
            set
            {
                _badge = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Badge)));
            }
        }

        private FontAwesomeIcon _icon;

        public FontAwesomeIcon Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
            }
        }

        private string _text;

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
    }
}
