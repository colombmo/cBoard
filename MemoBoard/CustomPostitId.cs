using System;
using System.Windows;

namespace MemoBoard {
    public static class CustomPostitId {

        public static readonly DependencyProperty MyPropertyProperty = DependencyProperty.RegisterAttached("CustomId",
            typeof(int), typeof(CustomPostitId), new FrameworkPropertyMetadata(null));

        public static int GetCustomId(UIElement element) {
            if (element == null)
                throw new ArgumentNullException("element");
            return (int)element.GetValue(MyPropertyProperty);
        }
        public static void SetCustomId(UIElement element, int value) {
            if (element == null)
                throw new ArgumentNullException("element");
            element.SetValue(MyPropertyProperty, value);
        }
    }
}