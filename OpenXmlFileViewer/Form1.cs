using System;
using System.Drawing;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using OpenXmlFileViewer.Extensions;

namespace OpenXmlFileViewer
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// DECLARATIONS
        /// </summary>
        private TreeNode MobjPreviousNode = null;
        private bool MbolInSelect = false;
        private string PackagePath = "";
        private int MintLastFindPos = 0;
        private FindDialog MobjFd = null;

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="args"></param>
        public Form1(string[] args)
        {
            InitializeComponent();
            lineNumberTextBox.XmlTextChanged += (o, e) =>
            {
                toolStripBtnSave.Enabled = true;
            };
            this.Text = "OpenXml File Viewer";
            if (args.Length > 0)
            {
                PackagePath = args[0];
                openFile();
            }
            treeView.KeyDown += TreeView_KeyDown;
            toolStripBtnDelete.Image = SystemIcons.Error.ToBitmap();
            removeTabPages();
            webBrowserCtrl.Navigate("about:blank");
        }

        private void TreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedNode();
            }
        }

        /// <summary>
        /// Removes/hides all the tab pages from the control
        /// </summary>
        private void removeTabPages()
        {
            // hide all the tabs
            foreach (TabPage page in tabControl.TabPages)
                tabControl.TabPages.Remove(page);
        }

        /// <summary>
        /// The user clicked the OPEN button
        /// </summary>
        /// <param name="PobjSender"></param>
        /// <param name="PobjEventArgs"></param>
        private void toolStripBtnOpen_Click(object PobjSender, EventArgs PobjEventArgs)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "All|*.doc*;*.xls*;*.ppt*;*.mip;*.simp|Word Documents|*.doc*|Excel Workbooks|*.xls*|PowerPoint Presentations|*.ppt*";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    PackagePath = dialog.FileName;
                    openFile();
                }
            }
        }

        /// <summary>
        /// CORE - here is where we open the file denoted by the MstrPath value
        /// </summary>
        private void openFile()
        {
            toolStripBtnOpen.Enabled = false;
            toolStripBtnClose.Enabled = true;
            this.Text = "[" + new FileInfo(PackagePath).Name + "]";
            // open the package
            using (var package = Package.Open(PackagePath, FileMode.Open, FileAccess.Read))
            {
                // setup the root node
                var rootNode = treeView.Nodes.Add("/", "/");
                // read all the parts
                foreach (ZipPackagePart LobjPart in package.GetParts())
                {
                    // build a path string and...
                    int LintLen = LobjPart.Uri.OriginalString.LastIndexOf("/");
                    string LstrKey = LobjPart.Uri.OriginalString;
                    string LstrPath = LstrKey.Substring(0, LintLen);
                    string LstrName = LstrKey.Substring(LintLen + 1);
                    // set the parent, then
                    TreeNode parentNode = rootNode;
                    if (LstrPath.Length != 0)
                        parentNode = FindClosestNode(LstrPath);
                    // add the node to the tree control
                    parentNode.Nodes.Add(LstrKey, LstrName);
                }
            }
            MobjPreviousNode = treeView.Nodes[0];
            treeView.Nodes[0].Expand();
        }

        /// <summary>
        /// Locates the closest node to the given key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private TreeNode FindClosestNode(string key)
        {
            // get the parts of the path
            string[] parts = key.Split(new string[1] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            string LstrPath = "";
            // grab the root node
            TreeNode lastNode = treeView.Nodes[0];
            // search through all the parts
            foreach (var part in parts)
            {
                // build the path
                LstrPath += "/" + part;
                // get the node with that path
                TreeNode LobjNode = FindNodeFromPathRecursively(treeView.Nodes[0], LstrPath);
                if (LobjNode != null)
                    lastNode = LobjNode;
                else
                    lastNode = lastNode.Nodes.Add(LstrPath, part);
            }
            // return the found node
            return lastNode;
        }

        /// <summary>
        /// Get the node with the given path
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private TreeNode FindNodeFromPathRecursively(TreeNode parent, string path)
        {
            foreach (TreeNode childNode in parent.Nodes)
            {
                var node = FindNodeFromPathRecursively(childNode, path);
                if (node != null)
                    return node;
            }
            var parentPath = parent.FullPath.Substring(1).Replace("\\", "/");
            if (parentPath == path)
                return parent;
            else
                return null;
        }

        /// <summary>
        /// Does the key for the given node exist
        /// </summary>
        /// <param name="PobjNode"></param>
        /// <param name="PstrKey"></param>
        /// <returns></returns>
        private bool HasKey(TreeNode PobjNode, string PstrKey)
        {
            foreach (TreeNode LobjNode in PobjNode.Nodes)
                if (HasKey(LobjNode, PstrKey))
                    return true;
            if (PobjNode.Nodes.ContainsKey(PstrKey))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Take the input XML and parse it so that it is
        /// indented in a readable format. We use this so
        /// that we can load the XML into the text editor
        /// in a clean format the user can read
        /// </summary>
        /// <param name="PstrInputXml"></param>
        /// <returns></returns>
        public string FormatXml(string PstrInputXml)
        {
            XmlDocument LobjDocument = new XmlDocument();
            // loas the string into the XML document
            LobjDocument.Load(new StringReader(PstrInputXml));
            MemoryStream LobjMemoryStream = new MemoryStream();
            StringBuilder LobjStringBuilder = null;
            // open the memory stream for input
            using (XmlTextWriter LobjWriter = new XmlTextWriter(LobjMemoryStream, Encoding.UTF8))
            {
                LobjWriter.Formatting = Formatting.Indented;
                // input the XML Document into the memory stream - formatted
                LobjDocument.Save(LobjWriter);
                LobjMemoryStream.Position = 0;
                // red the memory stram into the string builder
                StreamReader sr = new StreamReader(LobjMemoryStream);
                sr.BaseStream.Position = 0;
                LobjStringBuilder = new StringBuilder(sr.ReadToEnd());
            }
            // return the formatted - indented string text
            return LobjStringBuilder.ToString();
        }

        /// <summary>
        /// The user clicked CLOSE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripBtnClose_Click(object sender, EventArgs e)
        {
            // clean up everything
            treeView.Nodes.Clear();
            webBrowserCtrl.DocumentText = "";
            lineNumberTextBox.Text = "";
            this.Text = "OpenXml File Viewer";
            label1.Text = "[path]";
            toolStripBtnOpen.Enabled = true;
            toolStripBtnClose.Enabled = false;
            toolStripBtnSave.Enabled = false;
            toolStripBtnFind.Enabled = false;
        }

        /// <summary>
        /// The user clicked a node inthe TreeView
        /// </summary>
        /// <param name="PobjSender"></param>
        /// <param name="PobjEventArgs"></param>
        private void treeView1_AfterSelect_1(object PobjSender, TreeViewEventArgs PobjEventArgs)
        {
            if (MbolInSelect)
                return;

            toolStripBtnDelete.Enabled = true;

            // is the document dirty? And are we on the Edit tab
            if (toolStripBtnSave.Enabled && tabControl.SelectedTab == tabPage2)
            {
                DialogResult LobjDr = MessageBox.Show("Are you sure you want to switch? \n\n" +
                                                      "The currently loaded part [" + label1.Text + "] has not been saved.",
                                                      "Save Loaded Part",
                                                      MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (LobjDr == DialogResult.Yes)
                {
                    toolStripBtnSave_Click(null, null);
                }
                else if (LobjDr == DialogResult.Cancel)
                {
                    MbolInSelect = true;
                    treeView.SelectedNode = MobjPreviousNode;
                    MbolInSelect = false;
                    return;
                }
            }

            // reset the form
            MobjPreviousNode.BackColor = SystemColors.Window;
            MobjPreviousNode = PobjEventArgs.Node;
            MobjPreviousNode.BackColor = SystemColors.MenuHighlight;
            lineNumberTextBox.Text = "";
            webBrowserCtrl.DocumentText = "";
            toolStripBtnSave.Enabled = false;
            toolStripBtnFind.Enabled = false;
            label1.Text = PobjEventArgs.Node.FullPath.Substring(1).Replace("\\", "/");
            toolStripBtnExtract.Enabled = true; // always allow export

            // determine the PART type - if it is not VML or XML, then do not try to
            // read it.
            if (!PobjEventArgs.Node.FullPath.ToLower().EndsWith(".xml") && !PobjEventArgs.Node.FullPath.ToLower().EndsWith(".rels") &&
                !PobjEventArgs.Node.FullPath.ToLower().EndsWith(".vml"))
            {
                // hide the text panes - since this part cannot be shown
                removeTabPages();

                // if the type is an image, then we will show it
                if (IsImageType(PobjEventArgs.Node.FullPath))
                {
                    tabControl.TabPages.Add(tabPage3);
                    tabPage3.Select();
                    using (ZipPackage LobjZip = (ZipPackage)ZipPackage.Open(PackagePath, FileMode.Open, FileAccess.Read))
                    {
                        // get the URI for the part
                        string LstrUri = PobjEventArgs.Node.FullPath.Substring(1).Replace("\\", "/");
                        // grab the part
                        ZipPackagePart LobjPart = (ZipPackagePart)LobjZip.GetPart(new Uri(LstrUri, UriKind.Relative));
                        Stream LobjBaseStream = LobjPart.GetStream(FileMode.Open, FileAccess.Read);
                        pictureBox1.Image = new Bitmap(LobjBaseStream);
                        LobjBaseStream.Close();
                    }
                }
                return;
            }
            else
            {
                // show the text panes
                removeTabPages();
                tabControl.TabPages.Add(tabPage1);
                tabControl.TabPages.Add(tabPage2);
            }

            try
            {
                // open the part to read the XML
                using (ZipPackage LobjZip = (ZipPackage)ZipPackage.Open(PackagePath, FileMode.Open, FileAccess.Read))
                {
                    // get the URI for the part
                    string LstrUri = PobjEventArgs.Node.FullPath.Substring(1).Replace("\\", "/");
                    // grab the part
                    ZipPackagePart LobjPart = (ZipPackagePart)LobjZip.GetPart(new Uri(LstrUri, UriKind.Relative));
                    Stream LobjBaseStream = LobjPart.GetStream(FileMode.Open, FileAccess.Read);
                    var stream = new MemoryStream();
                    LobjBaseStream.CopyTo(stream);
                    LobjBaseStream.Close();
                    // load the stream into a string
                    stream.Position = 0;
                    var xmlContent = new StreamReader(stream, Encoding.UTF8).ReadToEnd();
                    webBrowserCtrl.DocumentText = FormatXml(xmlContent);
                    stream.Position = 0;
                    // format the string
                    lineNumberTextBox.Text = xmlContent;
                }
                toolStripBtnSave.Enabled = false;
                toolStripBtnFind.Enabled = true;
                this.Refresh();
            }
            catch { }
        }

        private static bool IsImageType(string type)
        {
            switch (type.Split('.').Last().ToLower())
            {
                case "jpeg":
                case "jpg":
                case "png":
                case "bmp":
                case "wmf":
                case "emf":
                    return true;
                default:
                    return false;
            }
        }

        private void DeleteSelectedNode()
        {
            var node = treeView.SelectedNode;
            if (node == null)
                return;

            DeleteRecursive(node);
        }

        private bool DeleteRecursive(TreeNode node)
        {
            if (node == null)
                return false;

            while (node.Nodes.Count > 0)
            {
                if (!DeleteRecursive(node.Nodes[0]))
                    return false;
            }

            bool deleted = false;
            using (ZipPackage LobjZip = (ZipPackage)ZipPackage.Open(PackagePath, FileMode.Open, FileAccess.ReadWrite))
            {
                // get the URI for the part
                string LstrUri = node.FullPath.Substring(1).Replace("\\", "/");
                // grab the part
                var nodeToDelete = new Uri(LstrUri, UriKind.Relative);
                deleted = LobjZip.TryDeletePartAndRelationships(nodeToDelete);
            }

            if (deleted || node.Nodes.Count == 0)
            {
                treeView.Nodes.Remove(node);
                return true;
            }
            return false;
        }

        /// <summary>
        /// User selected to SAVE the selected open part
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripBtnSave_Click(object sender, EventArgs e)
        {
            // open the package
            using (ZipPackage LobjZip = (ZipPackage)ZipPackage.Open(PackagePath, FileMode.Open, FileAccess.ReadWrite))
            {
                string LstrUri = label1.Text;
                ZipPackagePart LobjPart = (ZipPackagePart)LobjZip.GetPart(new Uri(LstrUri, UriKind.Relative));
                Stream LobjStream = LobjPart.GetStream(FileMode.Open, FileAccess.ReadWrite);
                LobjStream.SetLength(0);
                LobjStream.Flush();
                StreamWriter LobjSw = new StreamWriter(LobjStream);
                LobjSw.Write(lineNumberTextBox.Text);
                LobjSw.Close();
            }
            toolStripBtnSave.Enabled = false;
        }

        /// <summary>
        /// User clicked FIND
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripBtnFind_Click(object sender, EventArgs e)
        {
            // find dialog
            MobjFd = new FindDialog();
            MobjFd.TopMost = true;
            MobjFd.FindNext += (string PstrItem) =>
            {
                int LintIdx = lineNumberTextBox.Text.IndexOf(PstrItem, MintLastFindPos);
                int LintLen = PstrItem.Length;
                if (LintIdx >= 0)
                {
                    lineNumberTextBox.Select(LintIdx, LintLen);
                    MintLastFindPos = LintIdx + LintLen;
                }
                else
                {
                    MintLastFindPos = 0;
                }
                lineNumberTextBox.Select();
                lineNumberTextBox.Focus();
                this.Focus();
            };
            MobjFd.Reset += () =>
            {
                MintLastFindPos = 0;
            };
            MobjFd.Show();
        }

        /// <summary>
        /// The user pressed a Key, was it F3 - for Find Next
        /// </summary>
        /// <param name="PobjSender"></param>
        /// <param name="PobjKeyArgs"></param>
        private void Form1_KeyUp(object PobjSender, KeyEventArgs PobjKeyArgs)
        {
            if (PobjKeyArgs.KeyCode == Keys.F3 && toolStripBtnFind.Enabled)
            {
                if (MobjFd != null && MobjFd.Visible)
                    MobjFd.FindNextPoke();
                else
                    toolStripBtnFind_Click(null, null);
            }
        }

        /// <summary>
        /// User clicked EXPORT to export a part
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripBtnExtract_Click(object sender, EventArgs e)
        {
            // get the filename for the part
            string LstrFn = label1.Text.Substring(label1.Text.LastIndexOf('/')).Replace("/", "");
            // ask the user
            SaveFileDialog LobjSfd = new SaveFileDialog();
            LobjSfd.Filter = "All Files (*.*)|*.*";
            LobjSfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            LobjSfd.FileName = LobjSfd.InitialDirectory + "\\" + LstrFn;
            if (LobjSfd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return;

            // open the package
            using (ZipPackage LobjZip = (ZipPackage)ZipPackage.Open(PackagePath, FileMode.Open, FileAccess.Read))
            {
                if (new FileInfo(LobjSfd.FileName).Exists)
                    new FileInfo(LobjSfd.FileName).Delete();

                // get the uri
                string LstrUri = label1.Text;
                // grab the part
                ZipPackagePart LobjPart = (ZipPackagePart)LobjZip.GetPart(new Uri(LstrUri, UriKind.Relative));
                // write the part to disk
                StreamReader LobjSr = new StreamReader(LobjPart.GetStream(FileMode.Open, FileAccess.Read));
                BinaryWriter LobjSw = new BinaryWriter(new FileInfo(LobjSfd.FileName).Create());
                while (!LobjSr.EndOfStream)
                    LobjSw.Write(LobjSr.Read());
                LobjSw.Close();
                LobjSr.Close();
            }
        }

        private void toolStripBtnDelete_Click(object sender, EventArgs e)
        {
            DeleteSelectedNode();
        }
    }
}
