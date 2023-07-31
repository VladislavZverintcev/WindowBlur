using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AppDevTools.Addons
{
    public class WindowBlur
    {
        #region Enums

        #region Private
        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19,
        }

        private enum AccentState
        {
            ACCENT_DISABLED,
            ACCENT_ENABLE_GRADIENT,
            ACCENT_ENABLE_TRANSPARENTGRADIENT,
            ACCENT_ENABLE_BLURBEHIND,
            ACCENT_INVALID_STATE,
        }
        #endregion Private

        #endregion Enums

        #region Structures

        #region Private
        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }
        #endregion Private

        #endregion Structures

        #region Fields

        #region Private
        private Window? window;
        #endregion Private

        #endregion Fields

        #region Properties

        #region Public
        public static readonly DependencyProperty BlurProperty = 
            DependencyProperty.RegisterAttached("Blur", typeof(WindowBlur), typeof(WindowBlur), new PropertyMetadata(null, BlurChanged));

        public static readonly DependencyProperty IsEnabledProperty = 
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(WindowBlur), new PropertyMetadata(false, IsEnabledChanged));
        #endregion Public

        #endregion Properties

        #region Methods

        #region Private
        private static void BlurChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is Window window)
            {
                ((WindowBlur)e.OldValue)?.Detach();
                ((WindowBlur)e.NewValue)?.Attach(window);
            }
        }

        private static void IsEnabledChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is Window window)
            {
                if (true.Equals(e.OldValue))
                {
                    GetWindowBlur(window)?.Detach();
                    window.ClearValue(BlurProperty);
                }
                if (true.Equals(e.NewValue))
                {
                    WindowBlur blur = new();
                    blur.Attach(window);
                    window.SetValue(BlurProperty, blur);
                }
            }
        }

        private void Attach(Window window)
        {
            this.window = window;
            var source = (HwndSource)PresentationSource.FromVisual(window);
            if (source == null)
            {
                window.SourceInitialized += OnSourceInitialized;
            }
            else
            {
                AttachCore();
            }
        }

        private void Detach()
        {
            try
            {
                DetachCore();
            }
            finally
            {
                window = null;
            }
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            if (sender == null)
            {
                return;
            }

            ((Window)sender).SourceInitialized -= OnSourceInitialized;
            AttachCore();
        }

        private void AttachCore()
        {
            if (window == null)
            {
                return;
            }

            EnableBlur(window);

            static void EnableBlur(Window window)
            {
                WindowInteropHelper windowHelper = new(window);

                AccentPolicy accent = new(){ AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND };
                int accentStructSize = Marshal.SizeOf(accent);

                IntPtr accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);

                WindowCompositionAttributeData data = new()
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = accentStructSize,
                    Data = accentPtr
                };

                SetWindowCompositionAttribute(windowHelper.Handle, ref data);

                Marshal.FreeHGlobal(accentPtr);
            }
        }

        private void DetachCore()
        {
            if (window == null)
            {
                return;
            }

            window.SourceInitialized += OnSourceInitialized;
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
        #endregion Private

        #region Public
        public static void SetWindowBlur(DependencyObject obj, WindowBlur value)
        {
            obj.SetValue(BlurProperty, value);
        }

        public static WindowBlur GetWindowBlur(DependencyObject obj)
        {
            return (WindowBlur)obj.GetValue(BlurProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }
        #endregion Public

        #endregion Methods
    }
}