namespace PrsHelper.WinForms
{
    partial class BatchTemplateControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.splitContainerTopLevel = new System.Windows.Forms.SplitContainer();
            this.tabControlTemplates = new System.Windows.Forms.TabControl();
            this.tabPageBatch = new System.Windows.Forms.TabPage();
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.tabPageOptions = new System.Windows.Forms.TabPage();
            this.propertyGridOptions = new System.Windows.Forms.PropertyGrid();
            this.gbxCommand = new System.Windows.Forms.GroupBox();
            this.btnSaveBatchTemplate = new System.Windows.Forms.Button();
            this.btnSaveOptions = new System.Windows.Forms.Button();
            this.btnLoadBatchTemplate = new System.Windows.Forms.Button();
            this.btnLoadOptions = new System.Windows.Forms.Button();
            this.btnGenerateBatchFromTemplate = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerTopLevel)).BeginInit();
            this.splitContainerTopLevel.Panel1.SuspendLayout();
            this.splitContainerTopLevel.Panel2.SuspendLayout();
            this.splitContainerTopLevel.SuspendLayout();
            this.tabControlTemplates.SuspendLayout();
            this.tabPageBatch.SuspendLayout();
            this.tabPageOptions.SuspendLayout();
            this.gbxCommand.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainerTopLevel
            // 
            this.splitContainerTopLevel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerTopLevel.Location = new System.Drawing.Point(0, 0);
            this.splitContainerTopLevel.Name = "splitContainerTopLevel";
            // 
            // splitContainerTopLevel.Panel1
            // 
            this.splitContainerTopLevel.Panel1.Controls.Add(this.tabControlTemplates);
            // 
            // splitContainerTopLevel.Panel2
            // 
            this.splitContainerTopLevel.Panel2.Controls.Add(this.gbxCommand);
            this.splitContainerTopLevel.Size = new System.Drawing.Size(592, 563);
            this.splitContainerTopLevel.SplitterDistance = 288;
            this.splitContainerTopLevel.TabIndex = 1;
            // 
            // tabControlTemplates
            // 
            this.tabControlTemplates.Controls.Add(this.tabPageBatch);
            this.tabControlTemplates.Controls.Add(this.tabPageOptions);
            this.tabControlTemplates.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlTemplates.Location = new System.Drawing.Point(0, 0);
            this.tabControlTemplates.Name = "tabControlTemplates";
            this.tabControlTemplates.SelectedIndex = 0;
            this.tabControlTemplates.Size = new System.Drawing.Size(288, 563);
            this.tabControlTemplates.TabIndex = 0;
            // 
            // tabPageBatch
            // 
            this.tabPageBatch.Controls.Add(this.propertyGrid1);
            this.tabPageBatch.Location = new System.Drawing.Point(4, 24);
            this.tabPageBatch.Name = "tabPageBatch";
            this.tabPageBatch.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageBatch.Size = new System.Drawing.Size(280, 535);
            this.tabPageBatch.TabIndex = 0;
            this.tabPageBatch.Text = "Batch Template";
            this.tabPageBatch.UseVisualStyleBackColor = true;
            // 
            // propertyGrid1
            // 
            this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid1.Location = new System.Drawing.Point(3, 3);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.Size = new System.Drawing.Size(274, 529);
            this.propertyGrid1.TabIndex = 0;
            // 
            // tabPageOptions
            // 
            this.tabPageOptions.Controls.Add(this.propertyGridOptions);
            this.tabPageOptions.Location = new System.Drawing.Point(4, 24);
            this.tabPageOptions.Name = "tabPageOptions";
            this.tabPageOptions.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageOptions.Size = new System.Drawing.Size(364, 541);
            this.tabPageOptions.TabIndex = 1;
            this.tabPageOptions.Text = "Options";
            this.tabPageOptions.UseVisualStyleBackColor = true;
            // 
            // propertyGridOptions
            // 
            this.propertyGridOptions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGridOptions.Location = new System.Drawing.Point(3, 3);
            this.propertyGridOptions.Name = "propertyGridOptions";
            this.propertyGridOptions.Size = new System.Drawing.Size(358, 535);
            this.propertyGridOptions.TabIndex = 0;
            // 
            // gbxCommand
            // 
            this.gbxCommand.Controls.Add(this.btnGenerateBatchFromTemplate);
            this.gbxCommand.Controls.Add(this.btnLoadOptions);
            this.gbxCommand.Controls.Add(this.btnLoadBatchTemplate);
            this.gbxCommand.Controls.Add(this.btnSaveOptions);
            this.gbxCommand.Controls.Add(this.btnSaveBatchTemplate);
            this.gbxCommand.Dock = System.Windows.Forms.DockStyle.Top;
            this.gbxCommand.Location = new System.Drawing.Point(0, 0);
            this.gbxCommand.Name = "gbxCommand";
            this.gbxCommand.Size = new System.Drawing.Size(300, 556);
            this.gbxCommand.TabIndex = 0;
            this.gbxCommand.TabStop = false;
            this.gbxCommand.Text = "Commands";
            // 
            // btnSaveBatchTemplate
            // 
            this.btnSaveBatchTemplate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveBatchTemplate.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnSaveBatchTemplate.Location = new System.Drawing.Point(6, 27);
            this.btnSaveBatchTemplate.Name = "btnSaveBatchTemplate";
            this.btnSaveBatchTemplate.Size = new System.Drawing.Size(267, 23);
            this.btnSaveBatchTemplate.TabIndex = 0;
            this.btnSaveBatchTemplate.Text = "Save Batch Template";
            this.btnSaveBatchTemplate.UseVisualStyleBackColor = true;
            // 
            // btnSaveOptions
            // 
            this.btnSaveOptions.Location = new System.Drawing.Point(6, 85);
            this.btnSaveOptions.Name = "btnSaveOptions";
            this.btnSaveOptions.Size = new System.Drawing.Size(267, 23);
            this.btnSaveOptions.TabIndex = 3;
            this.btnSaveOptions.Text = "Save Options";
            this.btnSaveOptions.UseVisualStyleBackColor = true;
            // 
            // btnLoadBatchTemplate
            // 
            this.btnLoadBatchTemplate.Location = new System.Drawing.Point(6, 56);
            this.btnLoadBatchTemplate.Name = "btnLoadBatchTemplate";
            this.btnLoadBatchTemplate.Size = new System.Drawing.Size(267, 23);
            this.btnLoadBatchTemplate.TabIndex = 4;
            this.btnLoadBatchTemplate.Text = "Load Batch Template";
            this.btnLoadBatchTemplate.UseVisualStyleBackColor = true;
            // 
            // btnLoadOptions
            // 
            this.btnLoadOptions.Location = new System.Drawing.Point(6, 114);
            this.btnLoadOptions.Name = "btnLoadOptions";
            this.btnLoadOptions.Size = new System.Drawing.Size(267, 23);
            this.btnLoadOptions.TabIndex = 5;
            this.btnLoadOptions.Text = "Load Options";
            this.btnLoadOptions.UseVisualStyleBackColor = true;
            // 
            // btnGenerateBatchFromTemplate
            // 
            this.btnGenerateBatchFromTemplate.Location = new System.Drawing.Point(6, 188);
            this.btnGenerateBatchFromTemplate.Name = "btnGenerateBatchFromTemplate";
            this.btnGenerateBatchFromTemplate.Size = new System.Drawing.Size(267, 23);
            this.btnGenerateBatchFromTemplate.TabIndex = 6;
            this.btnGenerateBatchFromTemplate.Text = "Generate Testing Batch";
            this.btnGenerateBatchFromTemplate.UseVisualStyleBackColor = true;
            // 
            // BatchTemplateControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainerTopLevel);
            this.Name = "BatchTemplateControl";
            this.Size = new System.Drawing.Size(592, 563);
            this.Load += new System.EventHandler(this.BatchTemplate_Load);
            this.splitContainerTopLevel.Panel1.ResumeLayout(false);
            this.splitContainerTopLevel.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerTopLevel)).EndInit();
            this.splitContainerTopLevel.ResumeLayout(false);
            this.tabControlTemplates.ResumeLayout(false);
            this.tabPageBatch.ResumeLayout(false);
            this.tabPageOptions.ResumeLayout(false);
            this.gbxCommand.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private SplitContainer splitContainerTopLevel;
        private TabControl tabControlTemplates;
        private TabPage tabPageBatch;
        private PropertyGrid propertyGrid1;
        private TabPage tabPageOptions;
        private PropertyGrid propertyGridOptions;
        private GroupBox gbxCommand;
        private Button btnSaveBatchTemplate;
        private Button btnGenerateBatchFromTemplate;
        private Button btnLoadOptions;
        private Button btnLoadBatchTemplate;
        private Button btnSaveOptions;
    }
}
