namespace Editor
{
	partial class Editor
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.fileDialog = new System.Windows.Forms.OpenFileDialog();
			this.tree = new Aga.Controls.Tree.TreeViewAdv();
			this.nodeTextBox1 = new Aga.Controls.Tree.NodeControls.NodeTextBox();
			this.SuspendLayout();
			// 
			// fileDialog
			// 
			this.fileDialog.Filter = "Meta files|*.meta";
			// 
			// tree
			// 
			this.tree.AllowDrop = true;
			this.tree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tree.AutoRowHeight = true;
			this.tree.BackColor = System.Drawing.SystemColors.Window;
			this.tree.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.tree.Cursor = System.Windows.Forms.Cursors.Default;
			this.tree.DefaultToolTipProvider = null;
			this.tree.DisplayDraggingNodes = true;
			this.tree.DragDropMarkColor = System.Drawing.Color.Black;
			this.tree.LineColor = System.Drawing.SystemColors.ControlDark;
			this.tree.Location = new System.Drawing.Point(0, 24);
			this.tree.Model = null;
			this.tree.Name = "tree";
			this.tree.NodeControls.Add(this.nodeTextBox1);
			this.tree.Search.BackColor = System.Drawing.Color.Pink;
			this.tree.Search.FontColor = System.Drawing.Color.Black;
			this.tree.SelectedNode = null;
			this.tree.Size = new System.Drawing.Size(467, 267);
			this.tree.TabIndex = 0;
			this.tree.Text = "treeViewAdv1";
			// 
			// Editor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(479, 303);
			this.Controls.Add(this.tree);
			this.Name = "Editor";
			this.Text = "Editor";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.OpenFileDialog fileDialog;
		private Aga.Controls.Tree.TreeViewAdv tree;
		private Aga.Controls.Tree.NodeControls.NodeTextBox nodeTextBox1;
	}
}

