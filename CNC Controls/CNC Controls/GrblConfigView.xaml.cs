/*
 * GrblConfigView.xaml.cs - part of CNC Controls library for Grbl
 *
 * v0.12 / 2020-03-10 / Io Engineering (Terje Io)
 *
 */

/*

Copyright (c) 2018-2020, Io Engineering (Terje Io)
All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

· Redistributions of source code must retain the above copyright notice, this
list of conditions and the following disclaimer.

· Redistributions in binary form must reproduce the above copyright notice, this
list of conditions and the following disclaimer in the documentation and/or
other materials provided with the distribution.

· Neither the name of the copyright holder nor the names of its contributors may
be used to endorse or promote products derived from this software without
specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/

using System.Data;
using System.Windows;
using System.Windows.Controls;
using CNC.Core;

namespace CNC.Controls
{
    public partial class GrblConfigView : UserControl, ICNCView
    {
        private Widget curSetting = null;
        private GrblViewModel model = null;

        public GrblConfigView()
        {
            InitializeComponent();

            DataContextChanged += GrblConfigView_DataContextChanged;
        }

        private void GrblConfigView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is GrblViewModel)
                model = (GrblViewModel)e.OldValue;
        }

        private void ConfigView_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = new WidgetViewModel();
            dgrSettings.DataContext = GrblSettings.Settings;
            dgrSettings.SelectedIndex = 0;
        }

        #region Methods required by CNCView interface

        public ViewType ViewType { get { return ViewType.GRBLConfig; } }

        public void Activate(bool activate, ViewType chgMode)
        {
            if (model != null)
                btnSave.IsEnabled = !model.IsCheckMode;
        }

        public void CloseFile()
        {
        }
        public void Setup(UIViewModel model, AppConfig profile)
        {
        }

        #endregion

        #region UIEvents

        void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (curSetting != null)
                curSetting.Assign();
            GrblSettings.Save();
        }

        void btnReload_Click(object sender, RoutedEventArgs e)
        {
            using(new UIUtils.WaitCursor()) {
                GrblSettings.Get();
            }
        }

        void btnBackup_Click(object sender, RoutedEventArgs e)
        {
            GrblSettings.Backup(string.Format("{0}settings.txt", CNC.Core.Resources.Path));
        }

        private void dgrSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1)
            {
                DataRow row = ((DataRowView)e.AddedItems[0]).Row;
                txtDescription.Text = ((string)row["Description"]).Replace("\\n", "\r\n");
                if (curSetting != null)
                {
                    curSetting.Assign();
                    canvas.Children.Clear();
                    curSetting.Dispose();
                }

                curSetting = new Widget(this, new WidgetProperties(row), canvas);
                curSetting.IsEnabled = true;
            }
        }
        #endregion
    }
}
