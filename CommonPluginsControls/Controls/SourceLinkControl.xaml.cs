using CommonPlayniteShared.Commands;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CommonPluginsControls.Controls
{
    public partial class SourceLinkControl : UserControl
    {
        public static readonly DependencyProperty SourceLabelProperty =
            DependencyProperty.Register(
                nameof(SourceLabel),
                typeof(string),
                typeof(SourceLinkControl),
                new PropertyMetadata(string.Empty, OnSourceChanged));

        public static readonly DependencyProperty SourceUrlProperty =
            DependencyProperty.Register(
                nameof(SourceUrl),
                typeof(string),
                typeof(SourceLinkControl),
                new PropertyMetadata(string.Empty, OnSourceChanged));

        public static readonly DependencyProperty LabelTextProperty =
            DependencyProperty.Register(
                nameof(LabelText),
                typeof(string),
                typeof(SourceLinkControl),
                new PropertyMetadata("Source:"));

        public static readonly DependencyProperty NavigateCommandProperty =
            DependencyProperty.Register(
                nameof(NavigateCommand),
                typeof(ICommand),
                typeof(SourceLinkControl),
                new PropertyMetadata(GlobalCommands.NavigateUrlCommand));

        private static readonly DependencyPropertyKey HasSourcePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(HasSource),
                typeof(bool),
                typeof(SourceLinkControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty HasSourceProperty = HasSourcePropertyKey.DependencyProperty;

        public string SourceLabel
        {
            get { return (string)GetValue(SourceLabelProperty); }
            set { SetValue(SourceLabelProperty, value); }
        }

        public string SourceUrl
        {
            get { return (string)GetValue(SourceUrlProperty); }
            set { SetValue(SourceUrlProperty, value); }
        }

        public string LabelText
        {
            get { return (string)GetValue(LabelTextProperty); }
            set { SetValue(LabelTextProperty, value); }
        }

        public ICommand NavigateCommand
        {
            get { return (ICommand)GetValue(NavigateCommandProperty); }
            set { SetValue(NavigateCommandProperty, value); }
        }

        public bool HasSource
        {
            get { return (bool)GetValue(HasSourceProperty); }
            private set { SetValue(HasSourcePropertyKey, value); }
        }

        public SourceLinkControl()
        {
            InitializeComponent();
            UpdateHasSource();
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SourceLinkControl control)
            {
                control.UpdateHasSource();
            }
        }

        private void UpdateHasSource()
        {
            HasSource = !string.IsNullOrWhiteSpace(SourceLabel) && !string.IsNullOrWhiteSpace(SourceUrl);
        }
    }
}