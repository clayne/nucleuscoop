﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nucleus.Gaming.Cache;
using Nucleus.Gaming.Controls.SetupScreen;
using Nucleus.Gaming.Coop;
using Nucleus.Gaming.Tools.GlobalWindowMethods;
using Nucleus.Gaming.Tools.Steam;
using Nucleus.Gaming.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;


namespace Nucleus.Gaming.Controls
{
    public partial class ProfilesList : ControlListBox
    {
        private IniFile themeIni = Globals.ThemeConfigFile;

        private float _scale;
        public static ProfilesList Instance;
        public bool Locked = false;

        private Cursor hand_Cursor;
        private Cursor default_Cursor;

        private Color buttonsBackColor;
        public static readonly string PartialTitle = "Load profile:";

        private Pen borderPen;
        private Control parentControl;

        public ProfilesList(Control parent)
        {
            parentControl = parent;

            InitializeComponent();

            Name = "ProfilePanel";
            Size = new Size(300, 3);
            Location = new Point(0, 0);
            Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Visible = false;
            BorderStyle = BorderStyle.None;
            BackColor = Color.Transparent;

            buttonsBackColor = Color.FromArgb(int.Parse(themeIni.IniReadValue("Colors", "ButtonsBackground").Split(',')[0]),
                                                  int.Parse(themeIni.IniReadValue("Colors", "ButtonsBackground").Split(',')[1]),
                                                  int.Parse(themeIni.IniReadValue("Colors", "ButtonsBackground").Split(',')[2]),
                                                  int.Parse(themeIni.IniReadValue("Colors", "ButtonsBackground").Split(',')[3]));

            borderPen = new Pen(Color.FromArgb(int.Parse(themeIni.IniReadValue("Colors", "SetupScreenBorder").Split(',')[0]),
                                               int.Parse(themeIni.IniReadValue("Colors", "SetupScreenBorder").Split(',')[1]),
                                               int.Parse(themeIni.IniReadValue("Colors", "SetupScreenBorder").Split(',')[2])), 1.0f);

            default_Cursor = Theme_Settings.Default_Cursor;
            hand_Cursor = Theme_Settings.Hand_Cursor;

            Instance = this;
        }

        public void ProfileBtn_CheckedChanged(object sender, MouseEventArgs e)
        {
            if (Locked)
            {
                return;
            }

            Label selected = (Label)sender;

            if (e != null)
            {
                if (e.Button == MouseButtons.Right)
                {
                    if (File.Exists(Application.StartupPath + "\\Profiles Launcher.exe"))
                    {
                        DialogResult dialogResult = System.Windows.Forms.MessageBox.Show($"Do you want to export a desktop shortcut for this handler profile?", "Export handler profile shortcut", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (dialogResult == DialogResult.Yes)
                        {
                            string jsonString = File.ReadAllText(GameProfile.profilesPathList[int.Parse(selected.Name) - 1]);
                            JObject Jprofile = (JObject)JsonConvert.DeserializeObject(jsonString);
                            string userNotes = ((string)Jprofile["Notes"] != null && (string)Jprofile["Notes"] != "") ? (string)Jprofile["Notes"] : "";

                            string shortcutTitle = selected.Text.StartsWith("Load profile:") ? selected.Text.Split(':')[1] : selected.Text;
                            GameProfile.CreateShortcut(GameProfile.GameInfo.GameGuid, shortcutTitle, selected.Name, userNotes);
                        }
                    }

                    return;
                }
            }

            selected.BackColor = Color.Transparent;
            foreach (Control c in Controls)
            {
                if (c != selected && c.Text != "Unload")
                {
                    c.ForeColor = Color.WhiteSmoke;
                }

                if (e == null && c.Text == "Unload")
                {
                    c.ForeColor = Color.Gray;
                    selected.Dispose();//dummy control use to reset the unload "button/label"
                }
            }

            if ((selected.Text == "Unload" && selected.ForeColor == Color.Gray) || e == null)
            {
                Globals.PlayButton.Text = "START";
                Globals.PlayButton.Enabled = false;
                return;
            }

            if (selected.Text == "Unload")
            {
                selected.ForeColor = Color.Gray;
                GameProfile.Instance.Reset();
                Globals.MainOSD.Show(500, "Handler Profile Unloaded");
                return;
            }

            if (GameProfile.Instance.LoadGameProfile(int.Parse(selected.Name)))//GameProfile auto reset on load
            {
                Controls[int.Parse(selected.Name) - 1].ForeColor = Color.LightGreen;
                Label unloadBtn = Controls[Controls.Count - 1] as Label;
                unloadBtn.ForeColor = Color.Orange;
            }
        }

        public void Update_ProfilesList()
        {
            Controls.Clear();

            List<SizeF> sizes = new List<SizeF>();

            Size = new Size((int)(300 * _scale), (int)(3 * _scale));
            int offset = 5;

            Font font = new Font("Franklin Gothic", 12F, FontStyle.Regular, GraphicsUnit.Pixel, 0);

            for (int i = 0; i < GameProfile.profilesPathList.Count + 1; i++)
            {
                string text;
                offset = 5;

                if (i != GameProfile.profilesPathList.Count)
                {
                    string jsonString = File.ReadAllText(GameProfile.profilesPathList[i]);
                    JObject Jprofile = (JObject)JsonConvert.DeserializeObject(jsonString);

                    if ((string)Jprofile["Title"] != null && (string)Jprofile["Title"] != "")
                    {
                        text = (string)Jprofile["Title"];
                    }
                    else
                    {
                        text = $"{PartialTitle} {i + 1}";
                    }
                }
                else
                {
                    text = "Unload";
                }

                Label deleteBtn = new Label
                {
                    Anchor = AnchorStyles.Right,
                    Size = new Size((int)(20 * _scale), (int)(20 * _scale)),
                    Font = new Font("Lucida Console", (float)12, FontStyle.Bold, GraphicsUnit.Pixel, 0),
                    ForeColor = Color.Red,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Text = "X",
                    Cursor = hand_Cursor
                };

                ToolTip deleteTooltip = CustomToolTips.SetToolTip(deleteBtn, $"Delete handler profile {i + 1}.", new int[] { 190, 0, 0, 0 }, new int[] { 255, 255, 255, 255 });
                deleteBtn.Click += new EventHandler(DeleteBtn_Click);//Delete profile

                offset += deleteBtn.Width;

                Label previewBtn = new Label
                {
                    Anchor = AnchorStyles.Right,
                    Size = new Size((int)(13 * _scale), (int)(20 * _scale)),
                    Font = new Font("Franklin Gothic", (float)10, FontStyle.Regular, GraphicsUnit.Pixel, 0),
                    BackgroundImageLayout = ImageLayout.Zoom,
                    BackgroundImage = ImageCache.GetImage(Globals.ThemeFolder + "magnifier.png"),
                    BackColor = Color.Transparent,
                    ForeColor = Color.Green,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = hand_Cursor
                };

                ToolTip notesTooltip = CustomToolTips.SetToolTip(previewBtn, "Show handler profile content\nor user notes if available.", new int[] { 190, 0, 0, 0 }, new int[] { 255, 255, 255, 255 });
                previewBtn.Click += new EventHandler(Profile_Preview);//view profile event 

                offset += previewBtn.Width;

                Label profileBtn = new Label
                {
                    Name = (i + 1).ToString(),
                    Anchor = AnchorStyles.Left | AnchorStyles.Right,
                    FlatStyle = FlatStyle.Flat,
                    BackgroundImageLayout = ImageLayout.Zoom,
                    Font = font,
                    BackColor = buttonsBackColor,
                    ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Text = text,
                    Height = (int)(20 * _scale),
                    Cursor = hand_Cursor
                };

                string profileBtnToolTipText = File.Exists(Application.StartupPath + "\\Profiles Launcher.exe") ? $"Load handler profile {profileBtn.Name}. Right click to export a shortcut to desktop." : $"Load handler profile {profileBtn.Name}.";

                ToolTip loadTooltip = CustomToolTips.SetToolTip(profileBtn, profileBtnToolTipText, new int[] { 190, 0, 0, 0 }, new int[] { 255, 255, 255, 255 });

                profileBtn.MouseClick += new MouseEventHandler(ProfileBtn_CheckedChanged);

                if (i != GameProfile.profilesPathList.Count)
                {
                    deleteBtn.Location = new Point(profileBtn.Right - deleteBtn.Width, profileBtn.Location.Y);
                    previewBtn.Location = new Point(deleteBtn.Left - previewBtn.Width, deleteBtn.Location.Y);
                    previewBtn.Location = new Point(deleteBtn.Left - previewBtn.Width, deleteBtn.Location.Y);
                    profileBtn.Controls.Add(deleteBtn);
                    profileBtn.Controls.Add(previewBtn);
                }
                else
                {
                    profileBtn.ForeColor = Color.Gray;
                    ToolTip unloadTooltip = CustomToolTips.SetToolTip(profileBtn, "Unload current loaded handler profile.", new int[] { 190, 0, 0, 0 }, new int[] { 255, 255, 255, 255 });
                }

                using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(new Bitmap(1, 1)))
                {
                    sizes.Add(graphics.MeasureString(profileBtn.Text, profileBtn.Font, Size.Width, StringFormat.GenericDefault));
                }

                Height += profileBtn.Height + 1;

                Controls.Add(profileBtn);
            }

            var sortedSizes = sizes.OrderByDescending(x => x.Width).ToList();//Sort profiles titles by Width so the list Width is set to the max value
            Width = (int)((sortedSizes[0].Width) * _scale) + offset;

            Location = new Point((parentControl.Right - Width) + 1, parentControl.Top + 5/*+ parentControl.gameProfilesList_btn.Height / 2*//*+ parentControl.gameProfilesList_btn.Height / 2*/);

            try
            {
                if (Region != null)
                {
                    Region.Dispose();
                }

                Region = Region.FromHrgn(GlobalWindowMethods.CreateRoundRectRgn(-1, -10, Width + 20, Height, 18, 18));
            }
            catch
            {
                if (Region != null)
                {
                    Region.Dispose();
                }

                Region = Region.FromHrgn(GlobalWindowMethods.CreateRoundRectRgn(-1, -10, Width + 20, Height, 18, 12));
            }

            BringToFront();

            if (Controls.Count == 1)
            {
                Controls.Clear();
                Visible = false;
            }
        }


        //Show profile config or user notes in handler note "zoomed" textbox
        private void Profile_Preview(object sender, EventArgs e)
        {
            if (Locked)
            {
                return;
            }

            Label selected = (Label)sender;

            Control preview = (Control)selected.Parent;

            if (preview.Text == "Unload")
            {
                return;
            }

            string jsonString = File.ReadAllText(GameProfile.profilesPathList[int.Parse(preview.Name) - 1]);
            JObject Jprofile = (JObject)JsonConvert.DeserializeObject(jsonString);

            string text;

            if ((string)Jprofile["Notes"] != "" && (string)Jprofile["Notes"] != null)
            {
                text = (string)Jprofile["Notes"];
            }
            else
            {
                text = jsonString;
            }

            Globals.HandlerNotesZoom.Notes.Text = text;
            Globals.HandlerNotesZoom.Visible = true;
            Globals.HandlerNotesZoom.BringToFront();
        }


        private void DeleteBtn_Click(object sender, EventArgs e)//Delete game profile
        {
            if (Locked)
            {
                return;
            }

            Label deleteBtn = (Label)sender;

            DialogResult dialogResult = MessageBox.Show($"Are you sure you want to delete handler profile {deleteBtn.Parent.Name} ?", "Are you sure?!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (dialogResult == DialogResult.Yes)
            {
                File.Delete(GameProfile.profilesPathList[int.Parse(deleteBtn.Parent.Name) - 1]);

                List<FileInfo> profilesPath = Directory.GetParent(GameProfile.profilesPathList[int.Parse(deleteBtn.Parent.Name) - 1]).
                                              EnumerateFiles().OrderBy(s => int.Parse(Regex.Match(s.Name, @"\d+").Value)).ToList();

                for (int i = 0; i < profilesPath.Count(); i++)
                {
                    if (profilesPath[i].Name == $"Profile[{i + 1}].json")
                    {
                        continue;
                    }

                    File.Move(profilesPath[i].FullName, $@"{Directory.GetParent(profilesPath[i].FullName)}\Profile[{i + 1}].json");
                }

                # region Delete per game profile game files backup 

                string backupPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\NucleusCoop\Game Files Backup\{GameProfile.GameInfo.GameGuid}";

                string backupToDelete = $"{backupPath}\\Profile{deleteBtn.Parent.Name}";

                if (Directory.Exists(backupToDelete))
                {
                    Directory.Delete(backupToDelete, true);

                    string[] backupFilesFolders = Directory.GetDirectories(backupPath, "*", SearchOption.TopDirectoryOnly);
                    List<string> profileBackupsOnly = new List<string>();

                    for (int i = 0; i < backupFilesFolders.Length; i++)
                    {
                        string backupFolder = backupFilesFolders[i];

                        if (backupFolder.Contains($"Profile"))
                        {
                            profileBackupsOnly.Add(backupFolder);
                        }
                    }

                    List<string> profileBackupsOnlySorted = profileBackupsOnly.OrderBy(s => int.Parse(Regex.Match(s, @"\d+").Value)).ToList();

                    for (int i = 0; i < profileBackupsOnlySorted.Count; i++)
                    {
                        string toRename = profileBackupsOnlySorted[i];

                        if (toRename == $"{backupPath}\\Profile{i + 1}")
                        {
                            continue;
                        }

                        Directory.Move(toRename, $"{backupPath}\\Profile{i + 1}");
                    }
                }

                #endregion

                GameProfile.Instance.Reset();

                Update_ProfilesList();

                if (Controls.Count == 0)
                {
                    Visible = false;
                    Globals.ProfilesList_btn.Visible = false;
                }

                Globals.MainOSD.Show(500, "Handler Profile Deleted");
            }
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            //Graphics g = e.Graphics;
            //g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            //g.CompositingQuality = CompositingQuality.HighQuality;
            //g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            //g.SmoothingMode = SmoothingMode.AntiAlias;
            //g.DrawRectangle(borderPen, new Rectangle(1, 1, Width - 3, Height - 3));

            //g.DrawArc(borderPen, 0, 0, 16, 16, -90, -90);//Top left corner
            //g.DrawArc(borderPen, 0, Height - 18, 16, 16, 90, 90);//Bottom left corner

            //g.DrawArc(borderPen, Width - 18, 0, 16, 16, -90, 90);//Top Right corner   
            //g.DrawArc(borderPen, Width - 18, Height - 18, 16, 16, 90, -90);//Bottom Right corner 
            //string steamAssetPath = SteamFunctions.GetSteamPath() + "\\appcache\\librarycache\\" + GameProfile.Game.SteamID + "_library_hero.jpg";
            //if (File.Exists(steamAssetPath))
            //{
            //    //BackgroundImageLayout = ImageLayout.Zoom;
            //    //BackgroundImage = new Bitmap(steamAssetPath);
            //    g.DrawImageUnscaled(Image.FromFile(steamAssetPath), 0, 0);
            //    g.FillRectangle(new SolidBrush(Color.FromArgb(160, 0, 0, 0)), new Rectangle(0,0,Width,Height));
            //}


            Rectangle gradientBrushbounds = new Rectangle(0, 0, Width, Height);
            //Rectangle bounds = new Rectangle(0, 0, Width, Height);
            Graphics g = e.Graphics;

            Color color = Color.FromArgb(100, 0, 0, 0);// Color.FromArgb(120, 0, 0, 0);
            Color color2 = Color.FromArgb(120, 0, 0, 0);
            LinearGradientBrush lgb =
            new LinearGradientBrush(gradientBrushbounds, Color.Transparent, color, 90f);

            ColorBlend topcblend = new ColorBlend(3);
            topcblend.Colors = new Color[3] { Color.Transparent,color, color2};
            topcblend.Positions = new float[3] { 0f, 0.5f, 1f };

            lgb.InterpolationColors = topcblend;
            //lgb.SetBlendTriangularShape(.5f, 1.0f);
            g.FillRectangle(lgb, gradientBrushbounds);

        }

        public void UpdateSize(float scale)
        {
            _scale = scale;
        }
    }
}
