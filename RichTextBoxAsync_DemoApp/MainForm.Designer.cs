namespace RichTextBoxAsync_DemoApp
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.LoadFileButton = new System.Windows.Forms.Button();
            this.AnimationTestLabel = new System.Windows.Forms.Label();
            this.LoadFileTextBox = new System.Windows.Forms.TextBox();
            this.AnimationTestExplanationLabel = new System.Windows.Forms.Label();
            this.LoadFileLabel = new System.Windows.Forms.Label();
            this.StatusLabel = new System.Windows.Forms.Label();
            this.LoadingPictureBox = new System.Windows.Forms.PictureBox();
            this.InteractionTestButton2 = new System.Windows.Forms.Button();
            this.InteractionTestButton1 = new System.Windows.Forms.Button();
            this.RTBAsync = new RichTextBoxAsync_Lib.RichTextBoxAsync();
            ((System.ComponentModel.ISupportInitialize)(this.LoadingPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // LoadFileButton
            // 
            this.LoadFileButton.Location = new System.Drawing.Point(640, 31);
            this.LoadFileButton.Name = "LoadFileButton";
            this.LoadFileButton.Size = new System.Drawing.Size(75, 23);
            this.LoadFileButton.TabIndex = 4;
            this.LoadFileButton.Text = "Load file";
            this.LoadFileButton.UseVisualStyleBackColor = true;
            this.LoadFileButton.Click += new System.EventHandler(this.LoadFileButton_Click);
            // 
            // AnimationTestLabel
            // 
            this.AnimationTestLabel.AutoSize = true;
            this.AnimationTestLabel.Location = new System.Drawing.Point(304, 8);
            this.AnimationTestLabel.Name = "AnimationTestLabel";
            this.AnimationTestLabel.Size = new System.Drawing.Size(78, 13);
            this.AnimationTestLabel.TabIndex = 1;
            this.AnimationTestLabel.Text = "[animation test]";
            // 
            // LoadFileTextBox
            // 
            this.LoadFileTextBox.Location = new System.Drawing.Point(104, 32);
            this.LoadFileTextBox.Name = "LoadFileTextBox";
            this.LoadFileTextBox.Size = new System.Drawing.Size(536, 20);
            this.LoadFileTextBox.TabIndex = 3;
            // 
            // AnimationTestExplanationLabel
            // 
            this.AnimationTestExplanationLabel.AutoSize = true;
            this.AnimationTestExplanationLabel.Location = new System.Drawing.Point(8, 8);
            this.AnimationTestExplanationLabel.Name = "AnimationTestExplanationLabel";
            this.AnimationTestExplanationLabel.Size = new System.Drawing.Size(294, 13);
            this.AnimationTestExplanationLabel.TabIndex = 0;
            this.AnimationTestExplanationLabel.Text = "If this animation pauses, you\'ll know the UI thread is blocked:";
            // 
            // LoadFileLabel
            // 
            this.LoadFileLabel.AutoSize = true;
            this.LoadFileLabel.Location = new System.Drawing.Point(8, 35);
            this.LoadFileLabel.Name = "LoadFileLabel";
            this.LoadFileLabel.Size = new System.Drawing.Size(84, 13);
            this.LoadFileLabel.TabIndex = 2;
            this.LoadFileLabel.Text = "Load this .rtf file:";
            // 
            // StatusLabel
            // 
            this.StatusLabel.AutoSize = true;
            this.StatusLabel.Location = new System.Drawing.Point(728, 32);
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(41, 13);
            this.StatusLabel.TabIndex = 5;
            this.StatusLabel.Text = "[status]";
            // 
            // LoadingPictureBox
            // 
            this.LoadingPictureBox.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.LoadingPictureBox.BackColor = System.Drawing.SystemColors.Window;
            this.LoadingPictureBox.Image = ((System.Drawing.Image)(resources.GetObject("LoadingPictureBox.Image")));
            this.LoadingPictureBox.Location = new System.Drawing.Point(528, 328);
            this.LoadingPictureBox.Name = "LoadingPictureBox";
            this.LoadingPictureBox.Size = new System.Drawing.Size(64, 64);
            this.LoadingPictureBox.TabIndex = 10;
            this.LoadingPictureBox.TabStop = false;
            this.LoadingPictureBox.Visible = false;
            // 
            // InteractionTestButton2
            // 
            this.InteractionTestButton2.Location = new System.Drawing.Point(920, 32);
            this.InteractionTestButton2.Name = "InteractionTestButton2";
            this.InteractionTestButton2.Size = new System.Drawing.Size(171, 23);
            this.InteractionTestButton2.TabIndex = 7;
            this.InteractionTestButton2.Text = "Interaction test 2";
            this.InteractionTestButton2.UseVisualStyleBackColor = true;
            // 
            // InteractionTestButton1
            // 
            this.InteractionTestButton1.Location = new System.Drawing.Point(920, 8);
            this.InteractionTestButton1.Name = "InteractionTestButton1";
            this.InteractionTestButton1.Size = new System.Drawing.Size(171, 23);
            this.InteractionTestButton1.TabIndex = 6;
            this.InteractionTestButton1.Text = "Interaction test 1";
            this.InteractionTestButton1.UseVisualStyleBackColor = true;
            // 
            // RTBAsync
            // 
            this.RTBAsync.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.RTBAsync.BackColor = System.Drawing.SystemColors.Window;
            this.RTBAsync.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.RTBAsync.Location = new System.Drawing.Point(0, 64);
            this.RTBAsync.Name = "RTBAsync";
            this.RTBAsync.Size = new System.Drawing.Size(1100, 601);
            this.RTBAsync.TabIndex = 8;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 665);
            this.Controls.Add(this.InteractionTestButton2);
            this.Controls.Add(this.InteractionTestButton1);
            this.Controls.Add(this.LoadingPictureBox);
            this.Controls.Add(this.StatusLabel);
            this.Controls.Add(this.LoadFileTextBox);
            this.Controls.Add(this.LoadFileLabel);
            this.Controls.Add(this.AnimationTestLabel);
            this.Controls.Add(this.AnimationTestExplanationLabel);
            this.Controls.Add(this.RTBAsync);
            this.Controls.Add(this.LoadFileButton);
            this.Name = "MainForm";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.LoadingPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button LoadFileButton;
        private System.Windows.Forms.Label AnimationTestLabel;
        private RichTextBoxAsync_Lib.RichTextBoxAsync RTBAsync;
        private System.Windows.Forms.TextBox LoadFileTextBox;
        private System.Windows.Forms.Label AnimationTestExplanationLabel;
        private System.Windows.Forms.Label LoadFileLabel;
        private System.Windows.Forms.Label StatusLabel;
        private System.Windows.Forms.PictureBox LoadingPictureBox;
        private System.Windows.Forms.Button InteractionTestButton2;
        private System.Windows.Forms.Button InteractionTestButton1;
    }
}

