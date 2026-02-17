using CommonPlayniteShared.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CommonPluginsControls.PlayniteControls
{
    public partial class SearchBox : UserControl
    {
        private int _oldCaret;
        private bool _ignoreTextCallback;
        internal IInputElement PreviousFocus;

        // ── Text ────────────────────────────────────────────────────────────

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text), typeof(string), typeof(SearchBox),
                new PropertyMetadata(string.Empty, TextPropertyChangedCallback));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // ── ShowImage ────────────────────────────────────────────────────────

        public static readonly DependencyProperty ShowImageProperty =
            DependencyProperty.Register(
                nameof(ShowImage), typeof(bool), typeof(SearchBox),
                new PropertyMetadata(true, ShowImagePropertyChangedCallback));

        public bool ShowImage
        {
            get { return (bool)GetValue(ShowImageProperty); }
            set { SetValue(ShowImageProperty, value); }
        }

        // ── IsFocused ────────────────────────────────────────────────────────

        public new static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.Register(
                nameof(IsFocused), typeof(bool), typeof(SearchBox),
                new PropertyMetadata(false, IsFocusedPropertyChangedCallback));

        public new bool IsFocused
        {
            get { return (bool)GetValue(IsFocusedProperty); }
            set { SetValue(IsFocusedProperty, value); }
        }

        // ── TextChanged event ────────────────────────────────────────────────

        public event TextChangedEventHandler TextChanged
        {
            add { PART_TextInpuText.TextChanged += value; }
            remove { PART_TextInpuText.TextChanged -= value; }
        }

        // ── Constructor ──────────────────────────────────────────────────────

        public SearchBox()
        {
            InitializeComponent();

            PART_ClearTextIcon.MouseUp += ClearImage_MouseUp;

            PART_TextInpuText.TextChanged += TextFilter_TextChanged;
            PART_TextInpuText.KeyUp += TextFilter_KeyUp;
            PART_TextInpuText.GotFocus += OnTextInputFocusChanged;
            PART_TextInpuText.LostFocus += OnTextInputFocusChanged;

            BindingTools.SetBinding(
                PART_TextInpuText,
                TextBox.TextProperty,
                this,
                nameof(Text),
                mode: System.Windows.Data.BindingMode.OneWay,
                trigger: System.Windows.Data.UpdateSourceTrigger.PropertyChanged);

            UpdateIconStates();
        }

        // ── Public API ───────────────────────────────────────────────────────

        public void ClearFocus()
        {
            if (PreviousFocus != null)
            {
                Keyboard.Focus(PreviousFocus);
            }
            else
            {
                PART_TextInpuText.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }

            PreviousFocus = null;
            IsFocused = false;
        }

        // ── Private ──────────────────────────────────────────────────────────

        private void UpdateIconStates()
        {
            bool hasText = !string.IsNullOrEmpty(Text);
            bool inputFocused = PART_TextInpuText.IsFocused;

            PART_ClearTextIcon.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;

            if (inputFocused || hasText)
            {
                PART_SeachIcon.Visibility = Visibility.Collapsed;
            }
            else
            {
                PART_SeachIcon.Visibility = ShowImage ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void OnTextInputFocusChanged(object sender, RoutedEventArgs e)
        {
            UpdateIconStates();
        }

        private void ClearImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            PART_TextInpuText.Clear();
        }

        private void TextFilter_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.Enter)
            {
                ClearFocus();
            }
        }

        private void TextFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_ignoreTextCallback)
            {
                return;
            }

            _ignoreTextCallback = true;
            Text = PART_TextInpuText.Text;
            _ignoreTextCallback = false;

            UpdateIconStates();
        }

        // ── Callbacks ────────────────────────────────────────────────────────

        private static void TextPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var obj = (SearchBox)d;
            if (obj._ignoreTextCallback || obj.PART_TextInpuText == null)
            {
                return;
            }

            int savedCaret = obj._oldCaret;
            int textLength = obj.PART_TextInpuText.Text.Length;

            if (obj.PART_TextInpuText.CaretIndex == 0 && textLength > 0 && savedCaret != textLength)
            {
                obj.PART_TextInpuText.CaretIndex = savedCaret;
            }

            obj._oldCaret = obj.PART_TextInpuText.CaretIndex;
        }

        private static void ShowImagePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SearchBox)d).UpdateIconStates();
        }

        private static void IsFocusedPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var obj = (SearchBox)d;
            bool shouldFocus = (bool)e.NewValue;

            if (!shouldFocus && !obj.PART_TextInpuText.IsFocused)
            {
                return;
            }

            if (shouldFocus)
            {
                obj.PreviousFocus = Keyboard.FocusedElement;
                obj.PART_TextInpuText.Focus();
            }
            else
            {
                obj.ClearFocus();
            }

            obj.UpdateIconStates();
        }
    }
}