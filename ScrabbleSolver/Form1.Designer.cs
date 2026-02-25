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
            button2 = new Button();
            openFileDialog1 = new OpenFileDialog();
            button3 = new Button();
            movesTextBox = new RichTextBox();
            showMove = new Button();
            moveId = new TextBox();
            updateBoardButton = new Button();
            rackTextBox = new TextBox();
            calculateButton = new Button();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(551, 829);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 0;
            button1.Text = "Analyze";
            button1.UseVisualStyleBackColor = true;
            button1.Click += OnButtonClick;
            // 
            // output
            // 
            output.Font = new Font("Consolas", 10F);
            output.Location = new Point(12, 12);
            output.Name = "output";
            output.Size = new Size(776, 347);
            output.TabIndex = 1;
            output.Text = "";
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
            label1.Size = new Size(68, 15);
            label1.TabIndex = 3;
            label1.Text = "Threshold 1";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 411);
            label2.Name = "label2";
            label2.Size = new Size(68, 15);
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
            // button2
            // 
            button2.Location = new Point(470, 829);
            button2.Name = "button2";
            button2.Size = new Size(75, 23);
            button2.TabIndex = 6;
            button2.Text = "Browse";
            button2.UseVisualStyleBackColor = true;
            button2.Click += BrowseClick;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog";
            // 
            // button3
            // 
            button3.Location = new Point(632, 829);
            button3.Name = "button3";
            button3.Size = new Size(75, 23);
            button3.TabIndex = 7;
            button3.Text = "Solve";
            button3.UseVisualStyleBackColor = true;
            button3.Click += SolveClick;
            // 
            // movesTextBox
            // 
            movesTextBox.Font = new Font("Consolas", 10F);
            movesTextBox.Location = new Point(12, 437);
            movesTextBox.Name = "movesTextBox";
            movesTextBox.Size = new Size(776, 386);
            movesTextBox.TabIndex = 8;
            movesTextBox.Text = "";
            // 
            // showMove
            // 
            showMove.Location = new Point(713, 407);
            showMove.Name = "showMove";
            showMove.Size = new Size(75, 23);
            showMove.TabIndex = 9;
            showMove.Text = "Show Move";
            showMove.UseVisualStyleBackColor = true;
            showMove.Click += showMove_Click;
            // 
            // moveId
            // 
            moveId.Location = new Point(666, 408);
            moveId.Name = "moveId";
            moveId.Size = new Size(41, 23);
            moveId.TabIndex = 10;
            // 
            // updateBoardButton
            // 
            updateBoardButton.Location = new Point(713, 365);
            updateBoardButton.Name = "updateBoardButton";
            updateBoardButton.Size = new Size(75, 23);
            updateBoardButton.TabIndex = 11;
            updateBoardButton.Text = "Update Board";
            updateBoardButton.UseVisualStyleBackColor = true;
            updateBoardButton.Click += updateBoardButton_Click;
            // 
            // rackTextBox
            // 
            rackTextBox.Location = new Point(232, 365);
            rackTextBox.Name = "rackTextBox";
            rackTextBox.Size = new Size(354, 23);
            rackTextBox.TabIndex = 12;
            // 
            // calculateButton
            // 
            calculateButton.Location = new Point(713, 829);
            calculateButton.Name = "calculateButton";
            calculateButton.Size = new Size(75, 23);
            calculateButton.TabIndex = 13;
            calculateButton.Text = "Calculate";
            calculateButton.UseVisualStyleBackColor = true;
            calculateButton.Click += calculateButton_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 864);
            Controls.Add(calculateButton);
            Controls.Add(rackTextBox);
            Controls.Add(updateBoardButton);
            Controls.Add(moveId);
            Controls.Add(showMove);
            Controls.Add(movesTextBox);
            Controls.Add(button3);
            Controls.Add(button2);
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
        private Button button2;
        private OpenFileDialog openFileDialog1;
        private Button button3;
        private RichTextBox movesTextBox;
        private Button showMove;
        private TextBox moveId;
        private Button updateBoardButton;
        private TextBox rackTextBox;
        private Button calculateButton;
    }
}
