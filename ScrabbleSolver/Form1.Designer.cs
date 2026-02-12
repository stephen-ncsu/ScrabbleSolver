namespace ScrabbleSolver
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            button1 = new Button();
            output = new RichTextBox();
            threshold1TextBox = new TextBox();
            label1 = new Label();
            label2 = new Label();
            threshold2TextBox = new TextBox();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(713, 829);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 0;
            button1.Text = "button1";
            button1.UseVisualStyleBackColor = true;
            button1.Click += OnButtonClick;
            // 
            // output
            // 
            output.Location = new Point(12, 12);
            output.Name = "output";
            output.Size = new Size(776, 347);
            output.TabIndex = 1;
            output.Text = "";
            output.Font = new Font("Consolas", 10);
            // 
            // threshold1TextBox
            // 
            threshold1TextBox.Location = new Point(87, 379);
            threshold1TextBox.Name = "threshold1TextBox";
            threshold1TextBox.Size = new Size(100, 23);
            threshold1TextBox.TabIndex = 2;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 382);
            label1.Name = "label1";
            label1.Size = new Size(69, 15);
            label1.TabIndex = 3;
            label1.Text = "Threshold 1";
            label1.Click += label1_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 411);
            label2.Name = "label2";
            label2.Size = new Size(69, 15);
            label2.TabIndex = 5;
            label2.Text = "Threshold 2";
            // 
            // threshold2TextBox
            // 
            threshold2TextBox.Location = new Point(87, 408);
            threshold2TextBox.Name = "threshold2TextBox";
            threshold2TextBox.Size = new Size(100, 23);
            threshold2TextBox.TabIndex = 4;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 864);
            Controls.Add(label2);
            Controls.Add(threshold2TextBox);
            Controls.Add(label1);
            Controls.Add(threshold1TextBox);
            Controls.Add(output);
            Controls.Add(button1);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private RichTextBox output;
        private TextBox threshold1TextBox;
        private Label label1;
        private Label label2;
        private TextBox threshold2TextBox;
    }
}
