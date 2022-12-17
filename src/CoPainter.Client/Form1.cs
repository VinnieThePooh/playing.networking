namespace CoPainter.Client;

public partial class Form1 : Form
{
    private PictureBox picture;
    private RichTextBox logRichTextBox;

    public Form1()
    {
        InitializeComponent();
        Load += OnLoad;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        InitUiState();
        CreateBasicControls();
    }

    private void InitUiState()
    {
        Size = MinimumSize = MaximumSize = new Size(1200, 600);
        MaximizeBox = false;
    }

    private void CreateBasicControls()
    {
        var menuStrip = new MenuStrip();
        var dropDown = new ToolStripMenuItem { Text = "File" };

        var exitButton = new ToolStripButton { Text = "Exit" };
        exitButton.Click += ExitButtonOnClick;

        dropDown.DropDownItems.Add(exitButton);
        menuStrip.Items.Add(dropDown);
        Controls.Add(menuStrip);

        (var splitContainer, picture, logRichTextBox) = CreateSplitContainer();
        Controls.Add(splitContainer);
    }

    private (SplitContainer, PictureBox, RichTextBox)  CreateSplitContainer()
    {
        SplitContainer sc = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
        sc.SplitterDistance = 100;
        sc.IsSplitterFixed = true;

        var pictureBox = new PictureBox
            { BorderStyle = BorderStyle.None, Dock = DockStyle.Fill, Name = "paintBox" };

        sc.Panel1.Controls.Add(pictureBox);

        var richTextBox = new RichTextBox { Dock = DockStyle.Fill, Name = "logRichTextBox" };
        sc.Panel2.Controls.Add(richTextBox);

        return (sc, pictureBox, richTextBox);
    }

    private void ExitButtonOnClick(object? sender, EventArgs e)
    {
         //dispose all and exit
         Application.Exit();
    }
}