﻿using DisplayMagician.GameLibraries;
using DisplayMagician.Resources;
using DisplayMagicianShared;
using Manina.Windows.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DisplayMagician.UIForms
{
    public partial class ShortcutLibraryForm : Form
    {

        private ShortcutAdaptor _shortcutAdaptor = new ShortcutAdaptor();
        private ShortcutItem _selectedShortcut = null;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public ShortcutLibraryForm()
        {
            InitializeComponent();
            ilv_saved_shortcuts.MultiSelect = false;
            ilv_saved_shortcuts.ThumbnailSize = new Size(100,100);
            ilv_saved_shortcuts.AllowDrag = false;
            ilv_saved_shortcuts.AllowDrop = false;
            ilv_saved_shortcuts.SetRenderer(new ShortcutILVRenderer());
        }

        private void btn_back_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ShortcutLibraryForm_Load(object sender, EventArgs e)
        {
            // Refresh the profiles and the shortcut validity to start
            // The rest of the refreshing happens as the shortcuts are added
            // and deleted.
            ProfileRepository.IsPossibleRefresh();
            ShortcutRepository.IsValidRefresh();

            // Refresh the Shortcut Library UI
            RefreshShortcutLibraryUI();

            RemoveWarningIfShortcuts();
        }


        private void RefreshShortcutLibraryUI()
        {

            if (ShortcutRepository.ShortcutCount == 0)
                return;

            // Temporarily stop updating the saved_profiles listview
            ilv_saved_shortcuts.SuspendLayout();            

            ImageListViewItem newItem = null;
            ilv_saved_shortcuts.Items.Clear();

            foreach (ShortcutItem loadedShortcut in ShortcutRepository.AllShortcuts.OrderBy(s => s.Name))
            {
                // Ignore any shortcuts with incompatible game libraries
                if (loadedShortcut.Category == ShortcutCategory.Game && (!Enum.IsDefined(typeof(SupportedGameLibraryType), loadedShortcut.GameLibrary) || loadedShortcut.GameLibrary == SupportedGameLibraryType.Unknown))
                {
                    // Skip showing unknown game library items as we have no way to deal with them
                    logger.Warn( $"ShortcutLibraryForm/RefreshShortcutLibraryUI: Ignoring game shortcut {loadedShortcut.Name} as it's from a Game library this version doesn't support.");
                    continue;
                }

                newItem = new ImageListViewItem(loadedShortcut, loadedShortcut.Name);

                // Select it if its the selectedProfile
                if (_selectedShortcut is ShortcutItem && _selectedShortcut.Equals(loadedShortcut))
                {
                    newItem.Selected = true;
                    // Hide the run button if the shortcut isn't valid
                    if (_selectedShortcut.IsValid == ShortcutValidity.Warning || _selectedShortcut.IsValid == ShortcutValidity.Error)
                    {
                        btn_run.Visible = false;
                        cms_shortcuts.Items[1].Enabled = false;
                    }

                    else
                    {
                        btn_run.Visible = true;
                        cms_shortcuts.Items[1].Enabled = true;
                    }
                }

                //ilv_saved_profiles.Items.Add(newItem);
                ilv_saved_shortcuts.Items.Add(newItem, _shortcutAdaptor);
            }

    
            // Restart updating the saved_profiles listview
            ilv_saved_shortcuts.ResumeLayout();

        }
    
        private ShortcutItem GetShortcutFromName(string shortcutName)
        {
            return (from item in ShortcutRepository.AllShortcuts where item.Name == shortcutName select item).First();
        }

        private ShortcutItem GetShortcutFromUUID(string shortcutUUID)
        {
            return (from item in ShortcutRepository.AllShortcuts where item.UUID == shortcutUUID select item).First();
        }

        private void btn_save_Click(object sender, EventArgs e)
        {
            // Only do something if there is a shortcut selected
            if (_selectedShortcut == null)
            {
                if (ShortcutRepository.ShortcutCount > 0)
                {
                    MessageBox.Show(
                        @"You need to select a Game Shortcut in order to save a desktop shortcut to it. Please select a Game Shortcut then try again, or right-click on the Game Shortcut and select 'Save Shortcut to Desktop'.",
                        @"Select Game Shortcut", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                else
                {
                    MessageBox.Show(
                        @"You need to create a Game Shortcut before you can save a desktop shortcut to it. Please create a Game Shortcut by clicking the New button.",
                        @"Create Game Shortcut", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }
            else
            {

                // if shortcut is not valid then ask if the user
                // really wants to save it to desktop
                if (_selectedShortcut.IsValid == ShortcutValidity.Error || _selectedShortcut.IsValid == ShortcutValidity.Warning)
                {
                    // We ask the user of they still want to save the desktop shortcut
                    if (MessageBox.Show($"The shortcut '{_selectedShortcut.Name}' isn't valid for some reason so a desktop shortcut wouldn't work until the shortcut is fixed. Has your hardware or screen layout changed from when the shortcut was made? We recommend that you edit the shortcut to make it valid again, or reverse the hardware changes you made. Do you still want to save the desktop shortcut?", $"Still save the '{_selectedShortcut.Name}' Desktop Shortcut?", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.No)
                        return;
                }

                try
                {
                    // Set the Shortcut save folder to the Desktop as that's where people will want it most likely
                    dialog_save.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    // Try to set up some sensible suggestions for the Shortcut name
                    if (_selectedShortcut.AutoName)
                    {
                        if (_selectedShortcut.DisplayPermanence == ShortcutPermanence.Permanent)
                        {

                            dialog_save.FileName = _selectedShortcut.Name;
                        }
                        else
                        {
                            if (_selectedShortcut.Category == ShortcutCategory.Application)
                            {
                                dialog_save.FileName = String.Concat(Path.GetFileNameWithoutExtension(_selectedShortcut.ExecutableNameAndPath), @" (", _selectedShortcut.Name.ToLower(CultureInfo.InvariantCulture), @")");
                            }
                            else
                            {
                                dialog_save.FileName = _selectedShortcut.Name;
                            }
                        }
                    }
                    else
                    {
                        dialog_save.FileName = _selectedShortcut.Name;
                    }

                    // Show the Save Shortcut window
                    if (dialog_save.ShowDialog(this) == DialogResult.OK)
                    {
                        if (_selectedShortcut.CreateShortcut(dialog_save.FileName))
                        {
                            MessageBox.Show(
                                String.Format(Language.Shortcut_placed_successfully, dialog_save.FileName),
                                Language.Shortcut,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show(
                                Language.Failed_to_create_the_shortcut_Unexpected_exception_occurred,
                                Language.Shortcut,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
                        }

                        dialog_save.FileName = string.Empty;
                        //DialogResult = DialogResult.OK;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Language.Shortcut, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void RemoveWarningIfShortcuts()
        {
            if (ShortcutRepository.AllShortcuts.Count > 0)
                lbl_create_shortcut.Visible = false;
            else
                lbl_create_shortcut.Visible = true;
        }

        private void ilv_saved_shortcuts_ItemClick(object sender, ItemClickEventArgs e)
        {
            // This is the single click to select
            _selectedShortcut = GetShortcutFromName(e.Item.Text);

            // Hide the run button if the shortcut isn't valid
            if (_selectedShortcut.IsValid == ShortcutValidity.Warning || _selectedShortcut.IsValid == ShortcutValidity.Error)
            {
                btn_run.Visible = false;
                cms_shortcuts.Items[1].Enabled = false;
            }

            else
            {
                btn_run.Visible = true;
                cms_shortcuts.Items[1].Enabled = true;
            }

            if (e.Buttons == MouseButtons.Right)
            {
                cms_shortcuts.Show(ilv_saved_shortcuts,e.Location);
            }
        }

        private void ilv_saved_shortcuts_ItemDoubleClick(object sender, ItemClickEventArgs e)
        {
            // This is the double click to run
            _selectedShortcut = GetShortcutFromName(e.Item.Text);
            
            // Hide the run button if the shortcut isn't valid
            if (_selectedShortcut.IsValid == ShortcutValidity.Warning || _selectedShortcut.IsValid == ShortcutValidity.Error)
            {
                btn_run.Visible = false;
                cms_shortcuts.Items[1].Enabled = false;
            }

            else
            {
                btn_run.Visible = true;
                cms_shortcuts.Items[1].Enabled = true;
            }

            // Run the selected shortcut
            btn_run.PerformClick();
        }

        private void btn_new_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            var shortcutForm = new ShortcutForm(new ShortcutItem());
            //ShortcutRepository.IsValidRefresh();
            shortcutForm.ShowDialog(this);
            if (shortcutForm.DialogResult == DialogResult.OK)
            {
                ShortcutRepository.AddShortcut(shortcutForm.Shortcut);
                _selectedShortcut = shortcutForm.Shortcut;
                //ShortcutRepository.IsValidRefresh();
                RefreshShortcutLibraryUI();
            }
            this.Cursor = Cursors.Default;
            RemoveWarningIfShortcuts();

        }

        private void btn_edit_Click(object sender, EventArgs e)
        {
            if (_selectedShortcut == null)
            {
                if (ShortcutRepository.ShortcutCount > 0)
                {
                    MessageBox.Show(
                        @"You need to select a Game Shortcut in order to edit it. Please select a Game Shortcut then try again, or right-click on the Game Shortcut and select 'Edit Shortcut'.",
                        @"Select Game Shortcut", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                else
                {
                    MessageBox.Show(
                        @"You need to create a Game Shortcut before you can edit it. Please create a Game Shortcut by clicking the New button.",
                        @"Create Game Shortcut", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

            }

            if (ilv_saved_shortcuts.SelectedItems.Count > 0)
            {
                int currentIlvIndex = ilv_saved_shortcuts.SelectedItems[0].Index;
                string shortcutUUID = ilv_saved_shortcuts.Items[currentIlvIndex].EquipmentModel;
                _selectedShortcut = GetShortcutFromUUID(shortcutUUID);

                this.Cursor = Cursors.WaitCursor;

                // We need to stop ImageListView redrawing things before we're ready
                // This stops an exception when ILV is just too keen!


                var shortcutForm = new ShortcutForm(_selectedShortcut);
                //ilv_saved_shortcuts.SuspendLayout();
                shortcutForm.ShowDialog(this);
                if (shortcutForm.DialogResult == DialogResult.OK)
                {
                    RefreshShortcutLibraryUI();
                    // As this is an edit, we need to manually force saving the shortcut library
                    ShortcutRepository.SaveShortcuts();
                }

                this.Cursor = Cursors.Default;
            }
        }

        private void btn_delete_Click(object sender, EventArgs e)
        {
            if (_selectedShortcut == null)
            {
                if (ShortcutRepository.ShortcutCount > 0)
                {
                    MessageBox.Show(
                        @"You need to select a Game Shortcut in order to delete it. Please select a Game Shortcut then try again, or right-click on the Game Shortcut and select 'Delete Shortcut'.",
                        @"Select Game Shortcut", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                else
                {
                    MessageBox.Show(
                        @"You need to create a Game Shortcut before you can delete it. Please create a Game Shortcut by clicking the New button.",
                        @"Create Game Shortcut", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }

            if (MessageBox.Show($"Are you sure you want to delete the '{_selectedShortcut.Name}' Shortcut?", $"Delete '{_selectedShortcut.Name}' Shortcut?", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.No)
                return;

            // remove the profile from the imagelistview
            int currentIlvIndex = ilv_saved_shortcuts.SelectedItems[0].Index;
            ilv_saved_shortcuts.Items.RemoveAt(currentIlvIndex);

            // Remove the shortcut
            ShortcutRepository.RemoveShortcut(_selectedShortcut);
            _selectedShortcut = null;

            ShortcutRepository.IsValidRefresh();
            RefreshShortcutLibraryUI();
            RemoveWarningIfShortcuts();
        }

        private void btn_run_Click(object sender, EventArgs e)
        {
            if (_selectedShortcut == null)
            {
                if (ShortcutRepository.ShortcutCount > 0)
                {
                    MessageBox.Show(
                        @"You need to select a Game Shortcut in order to run it. Please select a Game Shortcut then try again, or right-click on the Game Shortcut and select 'Run Shortcut'. Please note you cannot run an invalid Game Shortcut.",
                        @"Select Game Shortcut", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                else
                {
                    MessageBox.Show(
                        @"You need to create a Game Shortcut in order to run it. Please create a Game Shortcut by clicking the New button.",
                        @"Create Game Shortcut", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }

            // Only run the if shortcut is valid
            if (_selectedShortcut.IsValid == ShortcutValidity.Warning || _selectedShortcut.IsValid == ShortcutValidity.Error)
            {
                // We tell the user the reason that we couldnt run the shortcut
                if (MessageBox.Show($"The shortcut '{_selectedShortcut.Name}' isn't valid for some reason so we cannot run the application or game. Has your hardware or screen layout changed from when the shortcut was made? We recommend that you edit the shortcut to make it valid again, or reverse the hardware changes you made. Do you want to do that now?", $"Edit the '{_selectedShortcut.Name}' Shortcut?", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.No)
                    return;
                else
                    btn_edit.PerformClick();
            }

            // Figure out the string we're going to use as the MaskedForm message
            string message = "";
            if (_selectedShortcut.Category.Equals(ShortcutCategory.Application))
                message = $"Running the {_selectedShortcut.ExecutableNameAndPath} application and waiting until you close it.";
            else if (_selectedShortcut.Category.Equals(ShortcutCategory.Game))
                message = $"Running the {_selectedShortcut.GameName} game and waiting until you close it.";

            if (!Program.AppProgramSettings.MinimiseOnStart)
            {
                // Create a Mask Control that will cover the ShortcutLibrary Window to lock
                lbl_mask.Text = message;
                lbl_mask.Location = new Point(0, 0);
                lbl_mask.Size = this.Size;
                lbl_mask.BackColor = Color.FromArgb(100, Color.Black);
                lbl_mask.BringToFront();
                lbl_mask.Visible = true;

                ilv_saved_shortcuts.SuspendLayout();
                ilv_saved_shortcuts.Refresh();

                // Get the MainForm so we can access the NotifyIcon on it.
                MainForm mainForm = (MainForm)this.Owner;

                // Run the shortcut
                ShortcutRepository.RunShortcut(_selectedShortcut, mainForm.notifyIcon);

                ilv_saved_shortcuts.ResumeLayout();

                // REmove the Masked Control to allow the user to start using DisplayMagician again.
                lbl_mask.Visible = false;
                lbl_mask.SendToBack();
            }
            else
            {
                // Run the shortcut
                ShortcutRepository.RunShortcut(_selectedShortcut, Program.AppMainForm.notifyIcon);
            }
        }

        private void ilv_saved_shortcuts_ItemHover(object sender, ItemHoverEventArgs e)
        {
            if (e.Item != null)
            {
                tt_selected.SetToolTip(ilv_saved_shortcuts, e.Item.Text);
            }
            else
            {
                tt_selected.RemoveAll();
            }
        }

        private void ShortcutLibraryForm_Activated(object sender, EventArgs e)
        {
            // Refresh the Shortcut Library UI
            RefreshShortcutLibraryUI();

            RemoveWarningIfShortcuts();
        }

        private void tsmi_save_to_desktop_Click(object sender, EventArgs e)
        {
            btn_save.PerformClick();
        }

        private void tsmi_run_Click(object sender, EventArgs e)
        {
            btn_run.PerformClick();
        }

        private void tsmi_edit_Click(object sender, EventArgs e)
        {
            btn_edit.PerformClick();
        }

        private void tsmi_delete_Click(object sender, EventArgs e)
        {
            btn_delete.PerformClick();
        }
    }
}
