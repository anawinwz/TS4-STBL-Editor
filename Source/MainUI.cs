﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;
using System.Globalization;
using System.Threading;
using System.IO;
using System.Xml;
using System.Reflection;
using s4pi.Package;
using s4pi.Interfaces;
using System.Numerics;
using s4pi.WrapperDealer;

namespace TS4_STBL_Editor
{
    public partial class MainUI : Form
    {
        private DataGridViewSettings settings0 = new DataGridViewSettings();
        private DataGridViewSettings settings1 = new DataGridViewSettings();

        public static List<StringHolder> copiedValuesStrHolders = new List<StringHolder>();

        #region s4pe
        public static bool openedFromSTBL_File = true;
        public static BigInteger packageElId = 0;
        public static IPackage imppkg = null;
        public static List<IResourceIndexEntry> lrie;
        public static IResource res;
        #endregion

        public MainUI()
        {
            InitializeComponent();

            toolTip1.AutoPopDelay = 5000;
            toolTip1.InitialDelay = 500;
            toolTip1.ReshowDelay = 500;
            toolTip1.ShowAlways = true;

            this.AllowDrop = true;

            switch (Thread.CurrentThread.CurrentUICulture.ThreeLetterWindowsLanguageName)
            {
                case "CHS":
                    toolTip1.SetToolTip(this.editBtn, "在新窗口中编辑");
                    toolTip1.SetToolTip(this.exportBtn, "导出到XML文件");
                    toolTip1.SetToolTip(this.importBtn, "从XML文件导入");
                    break;
                case "CHT":
                    toolTip1.SetToolTip(this.editBtn, "在新視窗中編輯");
                    toolTip1.SetToolTip(this.exportBtn, "匯出到XML檔案");
                    toolTip1.SetToolTip(this.importBtn, "從XML檔案匯入");
                    break;
                default:
                    toolTip1.SetToolTip(this.editBtn, "Edit in a new Window");
                    toolTip1.SetToolTip(this.exportBtn, "Export to XML file");
                    toolTip1.SetToolTip(this.importBtn, "Import from XML file");
                    break;
            }

            settings0.StoreSettings(dataGridView1, 0);
            settings1.StoreSettings(dataGridView1, 1);
        }

        private string publicPath = string.Empty;
        private bool pathOpened = false;
        private bool canAlsoSave = false;
        private bool isTextChanged = false;

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void openPackageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openPackageFromOpenFileDialog();
        }

        private void openPackageFromOpenFileDialog()
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openedFromSTBL_File = true;

            switch (Thread.CurrentThread.CurrentUICulture.ThreeLetterWindowsLanguageName)
            {
                case "CHS":
                case "ZHI":
                    openFileDialog1.Filter = "STBL文件 (*.stbl)|*.stbl|所有文件 (*.*)|*.*";
                    openFileDialog1.FilterIndex = 1;
                    openFileDialog1.Title = "选择STBL文件";
                    break;
                case "CHT":
                case "ZHH":
                case "ZHM":
                    openFileDialog1.Filter = "STBL檔案 (*.stbl)|*.stbl|所有檔案 (*.*)|*.*";
                    openFileDialog1.FilterIndex = 1;
                    openFileDialog1.Title = "選取STBL檔案";
                    break;
                default:
                    openFileDialog1.Filter = "STBL Files (*.stbl)|*.stbl|All Files (*.*)|*.*";
                    openFileDialog1.FilterIndex = 1;
                    openFileDialog1.Title = "Choose STBL File";
                    break;
            }

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                openSTBLfile(openFileDialog1.FileName);
            }
            else
            {
                switch (Thread.CurrentThread.CurrentUICulture.ThreeLetterWindowsLanguageName)
                {
                    case "CHS":
                    case "ZHI":
                        filenameLabel.Text = "未打开任何文件。";
                        break;
                    case "CHT":
                    case "ZHH":
                    case "ZHM":
                        filenameLabel.Text = "未開啟任何檔案。";
                        break;
                    default:
                        filenameLabel.Text = "No file is opened.";
                        break;
                }
                pathOpened = false;
            }
        }

        private void openSTBLfile(string pathToFile)
        {
            string stblFilePath = string.Empty;
            bool fileIsOpened = false;

            if (pathToFile != null)
            {
                stblFilePath = pathToFile;
                filenameLabel.Text = stblFilePath;
                publicPath = stblFilePath;
                pathOpened = fileIsOpened = true;
            }

            if (fileIsOpened)
            {
                //fileNameLbl.Text = stblFilePath;

                if (stblFilePath.IndexOf("220557DA") > 0)
                {

                    var assembly = Assembly.GetExecutingAssembly();
                    var resourceName = "TS4_STBL_Editor.LangCodesList.xml";

                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string result = reader.ReadToEnd();

                        string langId = stblFilePath.Substring(stblFilePath.IndexOf("220557DA") + 18, 2);

                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(result);
                        XmlNode root = doc.DocumentElement;

                        XmlNode node = root.SelectSingleNode("lang[id= \"0x" + langId + "\"]");

                        languageLable.Text = node["name"].InnerXml;
                    }
                }
                else
                {
                    string standardFileName = "S4_220557DA_80000000_0B84CB2FC430848A%%+STBL.stbl";
                    switch (Thread.CurrentThread.CurrentUICulture.ThreeLetterWindowsLanguageName)
                    {
                        case "CHS":
                        case "ZHI":
                            languageLable.Text = "语言类型不明确，文件名格式应类似" + standardFileName;
                            break;
                        case "CHT":
                        case "ZHH":
                        case "ZHM":
                            languageLable.Text = "語言類型不明確，檔案名格式應類似" + standardFileName;
                            break;
                        default:
                            languageLable.Text = "Unknown, file name is not like " + standardFileName;
                            break;
                    }
                }

                UseWaitCursor = true;
                progressBar1.Visible = true;
                progressBar1.Value = 0;

                ArrayList tempList = ReadAndAnalyzeSTBLFile(stblFilePath);
                STBLToDataGridView(tempList);

                UseWaitCursor = false;
                progressBar1.Visible = false;
            }
        }

        private void savePackageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pathOpened)
            {
                SaveSTBL(false, openedFromSTBL_File);
            }
            else if (canAlsoSave)
            {
                SaveSTBL(true, openedFromSTBL_File);
            }
            isTextChanged = false;
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveSTBL(true, openedFromSTBL_File);
            isTextChanged = false;
        }

        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AboutEditorForm about = new AboutEditorForm();
            about.ShowDialog();
            about.Dispose();
        }

        private void editBtn_Click(object sender, EventArgs e)
        {
            if (dataGridView1.DataSource != null)
            {
                Editor stblEditor = new Editor();
                progressBar1.Visible = true;
                progressBar1.Value = 0;

                stblEditor.updateProgressBar = delegate (double i)
                {
                    progressBar1.Value = (int)(progressBar1.Maximum * i);
                };

                DataTable dataTable = (DataTable)dataGridView1.DataSource;

                stblEditor.dataTable = dataTable;
                progressBar1.Visible = false;
                stblEditor.ShowDialog();
                dataGridView1.DataSource = stblEditor.dataTable;

                for (int k = 0; k < dataGridView1.Rows.Count; k++)
                {
                    dataGridView1.Rows[k].HeaderCell.Value = (k + 1).ToString();
                }

                dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.Rows.Count - 1;

                stblEditor.Dispose();

                isTextChanged = true;
            }
            else
            {
                openPackageFromOpenFileDialog();
            }
        }

        private void exportBtn_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                switch (Thread.CurrentThread.CurrentUICulture.ThreeLetterWindowsLanguageName)
                {
                    case "CHS":
                    case "ZHI":
                        MessageBox.Show("没有东西可以导出", "消息", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        break;
                    case "CHT":
                        MessageBox.Show("沒有東西可以匯出", "消息", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        break;
                    case "ZHH":
                    case "ZHM":
                        MessageBox.Show("無嘢可以匯出", "消息", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        break;
                    default:
                        MessageBox.Show("Nothing can be exported.", "Message", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        break;
                }
                return;
            }

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            switch (Thread.CurrentThread.CurrentUICulture.ThreeLetterWindowsLanguageName)
            {
                case "CHS":
                case "ZHI":
                    saveFileDialog1.Filter = "XML文件 (*.xml)|*.xml|所有文件 (*.*)|*.*";
                    saveFileDialog1.FilterIndex = 1;
                    saveFileDialog1.Title = "从XML文件导入";
                    break;
                case "CHT":
                case "ZHH":
                case "ZHM":
                    saveFileDialog1.Filter = "XML檔案 (*.xml)|*.xml|所有檔案 (*.*)|*.*";
                    saveFileDialog1.FilterIndex = 1;
                    saveFileDialog1.Title = "匯出到XML檔案";
                    break;
                default:
                    saveFileDialog1.Filter = "XML File (*.xml)|*.xml|All File (*.*)|*.*";
                    saveFileDialog1.FilterIndex = 1;
                    saveFileDialog1.Title = "Export to XML File";
                    break;
            }
            saveFileDialog1.FileName = filenameLabel.Text.Replace(".stbl", ".xml");

            ArrayList textResourceIDInString = new ArrayList();
            ArrayList textString = new ArrayList();

            string hexText;

            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                hexText = ((string)dataGridView1.Rows[i].Cells[0].Value).Replace("0x", "");
                textResourceIDInString.Add(hexText);
                textString.Add(dataGridView1.Rows[i].Cells[1].Value);
            }

            ArrayList tempList = new ArrayList();
            tempList.Add(textResourceIDInString);
            tempList.Add(textString);

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                WriteXML(saveFileDialog1.FileName, tempList);

        }

        private void importBtn_Click(object sender, EventArgs e)
        {
            string xmlFilePath;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            switch (Thread.CurrentThread.CurrentUICulture.ThreeLetterWindowsLanguageName)
            {
                case "CHS":
                case "ZHI":
                    openFileDialog1.Filter = "XML文件 (*.xml)|*.xml|所有文件 (*.*)|*.*";
                    openFileDialog1.FilterIndex = 1;
                    openFileDialog1.Title = "从XML文件导入";
                    break;
                case "CHT":
                case "ZHH":
                case "ZHM":
                    openFileDialog1.Filter = "XML檔案 (*.xml)|*.xml|所有檔案 (*.*)|*.*";
                    openFileDialog1.FilterIndex = 1;
                    openFileDialog1.Title = "從XML檔案匯入";
                    break;
                default:
                    openFileDialog1.Filter = "XML File (*.xml)|*.xml|All File (*.*)|*.*";
                    openFileDialog1.FilterIndex = 1;
                    openFileDialog1.Title = "Import from XML File";
                    break;
            }

            ArrayList tempList = new ArrayList();

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                xmlFilePath = openFileDialog1.FileName;
                tempList = ReadXML(xmlFilePath);
            }
            else return;

            if (!(bool)tempList[2])
            {
                return;
            }

            UseWaitCursor = true;
            progressBar1.Visible = true;
            progressBar1.Value = 0;

            XMLToDataGridView(tempList, xmlFilePath);

            UseWaitCursor = false;
            progressBar1.Visible = false;
            if (!pathOpened)
                canAlsoSave = true;
            else canAlsoSave = false;
            isTextChanged = true;
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            closeAndSavePackage();
        }

        private void closeAndSavePackage(bool save = true)
        {
            if (save)
            {
                if (isTextChanged && pathOpened)
                    SaveSTBL(false, openedFromSTBL_File);
                else if (isTextChanged && dataGridView1.Rows.Count > 0)
                    SaveSTBL(true, openedFromSTBL_File);
            }

            dataGridView1.DataSource = null;
            dataGridView1.Columns.Add("Column1", "");
            dataGridView1.Columns.Add("Column2", "");
            RestoreDataGridViewSettings();

            switch (Thread.CurrentThread.CurrentUICulture.ThreeLetterWindowsLanguageName)
            {
                case "CHS":
                case "ZHI":
                    filenameLabel.Text = "未打开任何文件。";
                    break;
                case "CHT":
                case "ZHH":
                case "ZHM":
                    filenameLabel.Text = "未開啟任何檔案。";
                    break;
                default:
                    filenameLabel.Text = "No file is opened.";
                    break;
            }

            languageLable.Text = string.Empty;
            publicPath = string.Empty;
            pathOpened = false;
            canAlsoSave = false;
            isTextChanged = false;


            openedFromSTBL_File = true;
            packageElId = 0;
            if (imppkg != null)
            {
                imppkg.Dispose();
                imppkg = null;
                lrie.Clear();
            }

            res = null;

        }

        private void MainUI_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files) Console.WriteLine(file);
            openSTBLfile(files[0]);
        }

        private void MainUI_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void showLangCodesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new LangCodesHelp()).ShowDialog();
        }

        private void multyInsertIntoFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainUI.copiedValuesStrHolders.Count > 0)
            {

                isTextChanged = false;
                closeAndSavePackage(false);

                OpenFileDialog openFileDialog1 = new OpenFileDialog();

                openFileDialog1.Multiselect = true;

                switch (Thread.CurrentThread.CurrentUICulture.ThreeLetterWindowsLanguageName)
                {
                    case "CHS":
                    case "ZHI":
                        openFileDialog1.Filter = "STBL文件 (*.stbl)|*.stbl|所有文件 (*.*)|*.*";
                        openFileDialog1.FilterIndex = 1;
                        openFileDialog1.Title = "选择STBL文件";
                        break;
                    case "CHT":
                    case "ZHH":
                    case "ZHM":
                        openFileDialog1.Filter = "STBL檔案 (*.stbl)|*.stbl|所有檔案 (*.*)|*.*";
                        openFileDialog1.FilterIndex = 1;
                        openFileDialog1.Title = "選取STBL檔案";
                        break;
                    default:
                        openFileDialog1.Filter = "STBL Files (*.stbl)|*.stbl|All Files (*.*)|*.*";
                        openFileDialog1.FilterIndex = 1;
                        openFileDialog1.Title = "Choose STBL File";
                        break;
                }

                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {

                    foreach (string fileName in openFileDialog1.FileNames)
                    {
                        openSTBLfile(fileName);

                        isTextChanged = true;

                        for (int x = 0; x < MainUI.copiedValuesStrHolders.Count; x++)
                        {
                            StringHolder copiedStrElement = MainUI.copiedValuesStrHolders[x];

                            DataTable dt = (DataTable)dataGridView1.DataSource;

                            var drArr = (from rowEl in dt.AsEnumerable()
                                         where rowEl.Field<string>(0) == copiedStrElement.textIDFld
                                         select rowEl);

                            if (drArr.Count() == 0)
                            {
                                DataRow dr = dt.NewRow();
                                dr[0] = copiedStrElement.textIDFld;
                                dr[1] = copiedStrElement.displayTextFld;
                                dt.Rows.Add(dr);
                                dataGridView1.Rows[dataGridView1.Rows.Count - 1].HeaderCell.Value = (dataGridView1.Rows.Count).ToString();

                            }
                            else
                            {
                                DataRow dr = drArr.First();
                                dr[1] = copiedStrElement.displayTextFld;
                            }
                        }

                        //break;

                        closeAndSavePackage();
                    }
                    isTextChanged = false;

                }
                MessageBox.Show("Done!");
            }
            else
            {
                switch (Thread.CurrentThread.CurrentUICulture.ThreeLetterWindowsLanguageName)
                {
                    case "CHS":
                    case "ZHI":
                        MessageBox.Show("您未复制任何字串。\r\n请先复制字串，然后再使用该选项向STBL文件大量插入已复制的字串！");
                        break;
                    case "CHT":
                    case "ZHH":
                    case "ZHM":
                        MessageBox.Show("您未複製任何字串。\r\n請先複製字串，然後再使用該選項向STBL文件大量插入已複製的字串！");
                        break;
                    default:
                        MessageBox.Show("You have not copied any string. \r\nCopy strings and use this option for mass insert of copied strings into STBL files!");
                        break;
                }
            }
        }



        private void openPackageFile(string pathToPackageFile)
        {
            imppkg = Package.OpenPackage(0, pathToPackageFile, true);

            lrie = imppkg.FindAll(x =>
            {
                return (x.ResourceType == 0x220557DA);
            });

            SelectSTBLfileinPackage f = new SelectSTBLfileinPackage(lrie, imppkg);
            f.ShowDialog();

            packageElId = BigInteger.Parse(f.selectedElement.Replace("0x", ""), NumberStyles.AllowHexSpecifier);

            var el = lrie.Find(x =>
            {
                return (x.Instance == packageElId);
            });

            res = WrapperDealer.GetResource(0, imppkg, el, true);

            openedFromSTBL_File = false;

            ArrayList tempList = ReadAndAnalyzeStream(res.Stream);
            STBLToDataGridView(tempList);

            pathOpened = true;

            //MessageBox.Show(packageElId.ToString());

            //imppkg.Dispose();
        }

        private void openpackageFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            switch (Thread.CurrentThread.CurrentUICulture.ThreeLetterWindowsLanguageName)
            {
                case "CHS":
                case "ZHI":
                    openFileDialog1.Filter = "STBL文件 (*.package)|*.package|所有文件 (*.*)|*.*";
                    openFileDialog1.FilterIndex = 1;
                    openFileDialog1.Title = "选择STBL文件";
                    break;
                case "CHT":
                case "ZHH":
                case "ZHM":
                    openFileDialog1.Filter = "STBL檔案 (*.package)|*.package|所有檔案 (*.*)|*.*";
                    openFileDialog1.FilterIndex = 1;
                    openFileDialog1.Title = "選取STBL檔案";
                    break;
                default:
                    openFileDialog1.Filter = "STBL Files (*.package)|*.package|All Files (*.*)|*.*";
                    openFileDialog1.FilterIndex = 1;
                    openFileDialog1.Title = "Choose .package File";
                    break;
            }

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                // openPackage(openFileDialog1.FileName);

                openPackageFile(openFileDialog1.FileName);

                //ArrayList tempList = ReadAndAnalyze(openFileDialog1.FileName);
                //STBLToDataGridView(tempList);
            }
            else
            {
                switch (Thread.CurrentThread.CurrentUICulture.ThreeLetterWindowsLanguageName)
                {
                    case "CHS":
                    case "ZHI":
                        filenameLabel.Text = "未打开任何文件。";
                        break;
                    case "CHT":
                    case "ZHH":
                    case "ZHM":
                        filenameLabel.Text = "未開啟任何檔案。";
                        break;
                    default:
                        filenameLabel.Text = "No file is opened.";
                        break;
                }
                pathOpened = false;
            }
        }
    }
}
