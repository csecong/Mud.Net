﻿using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Mud.Network;
using DataReceivedEventHandler = Mud.Network.DataReceivedEventHandler;

namespace Mud.Server.WPFTestApplication
{
    /// <summary>
    /// Interaction logic for ClientWindow.xaml
    /// </summary>
    public partial class ClientWindow : Window, IClient
    {
        public ClientWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;

            InputTextBox.Focus();

            IsConnected = true;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            Loaded -= OnLoaded;
        }

        private void SendButton_OnClick(object sender, RoutedEventArgs e)
        {
            string input = InputTextBox.Text.ToLower();
            DataReceived?.Invoke(this, input);
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        }

        private void InputTextBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                SendButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); // http://stackoverflow.com/questions/728432/how-to-programmatically-click-a-button-in-wpf
        }

        public event DataReceivedEventHandler DataReceived;

        public bool IsConnected { get; private set; }

        public bool ColorAccepted { get; set; }

        public void EchoOff()
        {
            // NOP
        }

        public void EchoOn()
        {
            // NOP
        }

        public void WriteData(string data)
        {
            //Paragraph paragraph = new Paragraph();
            //paragraph.Inlines.Add(data);
            //OutputRichTextBox.Document.Blocks.Add(paragraph);

            data = DateTime.Now.ToString("HH:mm:ss.ffffff") + data;

            Debug.WriteLine($"DATA:[{data}]");

            Dispatcher.InvokeAsync(() =>
            {
                Paragraph paragraph = new Paragraph();

                // Parse color code
                Brush currentColor = Brushes.LightGray;
                string remaining = data;
                while (true)
                {
                    int startIndex = remaining.IndexOf("%", StringComparison.OrdinalIgnoreCase);
                    //Debug.WriteLine($"StartIndex:[{startIndex}]");
                    if (startIndex >= 0)
                    {
                        string preceding = remaining.Substring(0, startIndex);
                        remaining = remaining.Substring(startIndex + 1);
                        //Debug.WriteLine($"Preceding:[{preceding}]");
                        //Debug.WriteLine($"Remaining:[{remaining}]");
                        AddColoredTextToParagraph(paragraph, currentColor, preceding);
                        //Debug.WriteLine($"AddColoredTextToParagraph preceding:[{preceding}] {currentColor}");
                        int endIndex = remaining.IndexOf("%", StringComparison.OrdinalIgnoreCase);
                        //Debug.WriteLine($"EndIndex:[{endIndex}]");
                        if (endIndex == 1) // %c%
                        {
                            string colorCode = remaining.Substring(0, endIndex);
                            //Debug.WriteLine($"ColorCode:[{colorCode}]");
                            Brush newColor = GetColor(colorCode);
                            if (newColor != null)
                            {
                                currentColor = newColor;
                                remaining = remaining.Substring(endIndex + 1);
                            }
                            else
                            {
                                //Debug.WriteLine($"AddColoredTextToParagraph % not color:[%] {currentColor}");
                                AddColoredTextToParagraph(paragraph, currentColor, "%");
                            }
                        }
                        else
                        {
                            //Debug.WriteLine($"AddColoredTextToParagraph %:[%] {currentColor}");
                            AddColoredTextToParagraph(paragraph, currentColor, "%");
                        }
                    }
                    else
                    {
                        //Debug.WriteLine($"AddColoredTextToParagraph remaining:[{remaining}] {currentColor}");
                        AddColoredTextToParagraph(paragraph, currentColor, remaining);
                        break;
                    }
                }
                OutputRichTextBox.Document.Blocks.Add(paragraph);
                OutputScrollViewer.ScrollToBottom();
            });
        }

        private void AddColoredTextToParagraph(Paragraph paragraph, Brush color, string text)
        {
            paragraph.Inlines.Add(new Run(text)
            {
                Foreground = color
            });
        }

        private Brush GetColor(string colorCode)
        {
            if (!ColorAccepted)
                return Brushes.LightGray;
            if (colorCode == "x")
                return Brushes.LightGray;
            // Normal
            if (colorCode == "r")
                return Brushes.DarkRed;
            if (colorCode == "g")
                return Brushes.DarkGreen;
            if (colorCode == "y")
                return Brushes.Yellow;
            if (colorCode == "b")
                return Brushes.DarkBlue;
            if (colorCode == "m")
                return Brushes.DarkMagenta;
            if (colorCode == "c")
                return Brushes.DarkCyan;
            if (colorCode == "w")
                return Brushes.DarkGray;
            // Light
            if (colorCode == "R")
                return Brushes.Red;
            if (colorCode == "G")
                return Brushes.Green;
            if (colorCode == "Y")
                return Brushes.LightGoldenrodYellow;
            if (colorCode == "B")
                return Brushes.Blue;
            if (colorCode == "M")
                return Brushes.Magenta;
            if (colorCode == "C")
                return Brushes.Cyan;
            if (colorCode == "W")
                return Brushes.White;
            if (colorCode == "D")
                return Brushes.Gray;
            return null;
        }

        public void Disconnect()
        {
            WriteData("Disconnected");
            //
            IsConnected = false;
        }
    }
}
