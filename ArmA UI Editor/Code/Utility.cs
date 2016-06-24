﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace ArmA_UI_Editor.Code
{
    public static class Utility
    {
        public static System.IO.Stream ToStream(this string str)
        {
            System.IO.MemoryStream stream = new System.IO.MemoryStream();
            System.IO.StreamWriter writer = new System.IO.StreamWriter(stream);
            writer.Write(str);
            writer.Flush();
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            return stream;
        }
        public static System.Windows.Rect GetCanvasMetrics(this System.Windows.UIElement el)
        {
            var rect = new System.Windows.Rect();
            rect.X = System.Windows.Controls.Canvas.GetLeft(el);
            rect.Y = System.Windows.Controls.Canvas.GetTop(el);
            rect.Width = System.Windows.Controls.Canvas.GetRight(el) - rect.X;
            rect.Height = System.Windows.Controls.Canvas.GetBottom(el) - rect.Y;
            return rect;
        }
        public static void SetCanvasMetrics(this System.Windows.UIElement el, System.Windows.Rect rect)
        {
            System.Windows.Controls.Canvas.SetLeft(el, rect.Left);
            System.Windows.Controls.Canvas.SetTop(el, rect.Top);
            System.Windows.Controls.Canvas.SetRight(el, rect.Right);
            System.Windows.Controls.Canvas.SetBottom(el, rect.Bottom);
        }
        public static T FindParentInHirarchy<T>(this System.Windows.FrameworkElement el) where T : System.Windows.FrameworkElement
        {
            while(el != null)
            {
                if(el.Parent is T)
                {
                    return el as T;
                }
                else if(el.Parent is System.Windows.FrameworkElement)
                {
                    el = el.Parent as System.Windows.FrameworkElement;
                }
                else
                {
                    return null;
                }
            }
            return null;
        }
        public static bool IsNumeric(this string s)
        {
            bool dot = false;
            foreach (var c in s)
            {
                if (!char.IsDigit(c))
                {
                    if (c == '.' && !dot)
                    {
                        dot = true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public static bool IsValidIdentifier(this string s)
        {
            bool allowDigits = false;
            foreach (var c in s)
            {
                if(allowDigits)
                {
                    if (!char.IsLetterOrDigit(c))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!char.IsLetter(c))
                    {
                        return false;
                    }
                    allowDigits = true;
                }
            }
            return true;
        }


        public static void tb_PreviewTextInput_Numeric_DoHandle(object sender, TextCompositionEventArgs e, Action a)
        {
            TextBox tb = sender as TextBox;
            if (e.Text.Contains('\r'))
            {
                if (tb.Text.Length == 0)
                    return;
                a.Invoke();
            }
            else
            {
                var currentSelection = tb.SelectionStart;
                string text = tb.Text;
                if (tb.SelectionLength > 0)
                {
                    text = text.Remove(tb.SelectionStart, tb.SelectionLength);
                }
                text = text.Insert(tb.SelectionStart, e.Text);
                currentSelection += e.Text.Length;
                if (text.IsNumeric())
                {
                    tb.Text = text;
                    tb.SelectionStart = currentSelection;
                }
            }
            e.Handled = true;
        }
        public static void tb_PreviewTextInput_Ident_DoHandle(object sender, TextCompositionEventArgs e, Action a)
        {
            if (e.Text.Contains('\r'))
            {
                TextBox tb = sender as TextBox;
                string text = tb.Text.Trim(new[] { ' ' });
                if (!text.IsValidIdentifier())
                {
                    MessageBox.Show("Non-Valid Identifier");
                }
                else
                {
                    a.Invoke();
                }
                e.Handled = true;
            }
        }
    }
}
