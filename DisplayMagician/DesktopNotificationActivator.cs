﻿using System;
using System.Runtime.InteropServices;
using DesktopNotifications;
using Microsoft.QueryStringDotNET;
using System.Windows.Forms;
using DisplayMagician.UIForms;

namespace DisplayMagician
{
    // The GUID must be unique to your app. Create a new GUID if copying this code.
    [ClassInterface(ClassInterfaceType.None)]
    [ComSourceInterfaces(typeof(INotificationActivationCallback))]
    [Guid("56F14154-6339-4B94-8B82-80F78D5BCEAF"), ComVisible(true)]
    public class DesktopNotificationActivator : NotificationActivator
    {
        public override void OnActivated(string arguments, NotificationUserInput userInput, string appUserModelId)
        {
            // Invoke the code we're running on the UI Thread to avoid
            // cross thread exceptions
            Program.AppMainForm.Invoke((MethodInvoker)delegate
            {
                // This code is running on the main UI thread!
                // Parse the query string (using NuGet package QueryString.NET)
                QueryString args = QueryString.Parse(arguments);

                foreach (QueryStringParameter myArg in args)
                {
                    if (myArg.Name.Equals("action",StringComparison.OrdinalIgnoreCase))
                    {
                        // See what action is being requested 
                        switch (args["action"].ToLowerInvariant())
                        {
                            // Open the image
                            case "open":

                                // Open the Main DisplayMagician Window
                                Program.AppMainForm.openApplicationWindow();
                                break;

                            // Background: Quick reply to the conversation
                            case "exit":

                                // Exit the application (overriding the close restriction)
                                Program.AppMainForm.exitApplication();
                                break;

                            case "stop":

                                MessageBox.Show("User just asked DisplayMagician to stop monitoring the game");
                                break;

                            default:
                                break;
                        }
                    }
                }
            });
        }

        private void OpenWindowIfNeeded()
        {
            // Make sure we have a window open (in case user clicked toast while app closed)
            if (Program.AppMainForm == null)
            {
                Program.AppMainForm = new MainForm();
            }

            // Activate the window, bringing it to focus
            Program.AppMainForm.openApplicationWindow();
        }
    }
}
