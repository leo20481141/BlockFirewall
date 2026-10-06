using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

public class MainForm : Form
{
  Panel main;
  Label pathLabel;
  Panel pathInputWrapper;
  TextBox pathInput;
  Button pathBrowse;
  Panel blockWrapper;
  Button blockButton;

  Panel blocking;
  TextBox blockingInfo;
  Panel blockingProgress;
  Button blockingBack;
  Button blockingClose;

  Color progressCompleteColor;
  Color progressIncompleteColor;

  public MainForm()
  {
    this.Text = "Bloquear Firewall";
    this.Width = 400;
    this.Height = 300;
    this.MinimumSize = new Size(this.Width, this.Height);

    main = new Panel();
    pathLabel = new Label();
    pathInputWrapper = new Panel();
    pathInput = new TextBox();
    pathBrowse = new Button();
    blockWrapper = new Panel();
    blockButton = new Button();

    pathInputWrapper.Controls.Add(pathInput);
    pathInputWrapper.Controls.Add(pathBrowse);
    blockWrapper.Controls.Add(blockButton);
    main.Controls.Add(pathLabel);
    main.Controls.Add(pathInputWrapper);
    main.Controls.Add(blockWrapper);
    this.Controls.Add(main);

    main.Dock = DockStyle.Fill;
    int top = 0;
    pathLabel.Text = "Carpeta";
    top += pathLabel.Height;

    pathInputWrapper.Width = main.DisplayRectangle.Width;
    pathInputWrapper.Height = 20;
    pathInputWrapper.Top = top;
    pathInputWrapper.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
    top += pathInputWrapper.Height;

    pathInput.Width = pathInputWrapper.Width - 60;
    pathInput.Height = pathInputWrapper.Height;
    pathInput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

    pathBrowse.Text = "Examinar";
    pathBrowse.Dock = DockStyle.Right;
    pathBrowse.Width = 60;
    pathBrowse.MouseDown += (s, e) => Browse();

    blockWrapper.Width = main.DisplayRectangle.Width;
    blockWrapper.Height = main.DisplayRectangle.Height - top;
    blockWrapper.Top = top;
    blockWrapper.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

    blockButton.Text = "Bloquear";
    blockButton.Top = (blockWrapper.DisplayRectangle.Height - blockButton.Height) / 2;
    blockButton.Left = (blockWrapper.DisplayRectangle.Width - blockButton.Width) / 2;
    blockButton.Anchor = AnchorStyles.None;
    blockButton.MouseDown += (s, e) => Block();

    blocking = new Panel();
    blockingInfo = new TextBox();
    blockingProgress = new Panel();
    blockingBack = new Button();
    blockingClose = new Button();

    blocking.Controls.Add(blockingInfo);
    blocking.Controls.Add(blockingProgress);
    blocking.Controls.Add(blockingBack);
    blocking.Controls.Add(blockingClose);
    this.Controls.Add(blocking);

    blocking.Left = 10;
    blocking.Top = 10;
    blocking.Width = this.DisplayRectangle.Width - 20;
    blocking.Height = this.DisplayRectangle.Height - 20;
    blocking.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;

    blockingClose.Text = "Cerrar";
    blockingClose.Left = blocking.Width - blockingClose.Width;
    blockingClose.Top = blocking.Height - blockingClose.Height;
    blockingClose.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
    blockingClose.MouseDown += (s, e) => Quit();

    blockingBack.Text = "Volver";
    blockingBack.Left = blockingClose.Left - blockingBack.Width;
    blockingBack.Top = blocking.Height - blockingBack.Height;
    blockingBack.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
    blockingBack.MouseDown += (s, e) => Back();

    progressCompleteColor = Color.FromArgb(255, 20, 60, 255);
    progressIncompleteColor = Color.FromArgb(255, 200, 230, 255);
    blockingProgress.BackColor = progressIncompleteColor;
    blockingProgress.Width = blocking.Width;
    blockingProgress.Height = 4;
    blockingProgress.Top = blockingBack.Top - 8;
    blockingProgress.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

    blockingInfo.ReadOnly = true;
    blockingInfo.Multiline = true;
    blockingInfo.WordWrap = false;
    blockingInfo.ScrollBars = ScrollBars.Both;
    blockingInfo.Width = blocking.Width;
    blockingInfo.Height = blockingBack.Top - 12;
    blockingInfo.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;

    blocking.Hide();
  }

  void Browse()
  {
    FolderBrowserDialog browser;
    DialogResult result;

    browser = new FolderBrowserDialog();
    browser.Description = "Busca la carpeta del programa.";
    browser.ShowNewFolderButton = true;
    browser.RootFolder = Environment.SpecialFolder.MyComputer;
    result = browser.ShowDialog();

    if(result != DialogResult.OK) return;
    if(string.IsNullOrEmpty(browser.SelectedPath)) return;

    pathInput.Text = browser.SelectedPath;
  }

  void Block()
  {
    string script = "";
    string batFileName;
    Process process;
    ProcessStartInfo psi;

    if(!Directory.Exists(pathInput.Text))
    {
      MessageBox.Show("La carpeta seleccionada no existe o no es un directorio.", "Error Carpeta Seleccionada", MessageBoxButtons.OK, MessageBoxIcon.Error);
      return;
    }

    this.FormClosing += PreventQuit;
    blockingInfo.Text = "";
    blockingBack.Enabled = false;
    blockingClose.Enabled = false;
    blockingProgress.BackColor = progressIncompleteColor;

    main.Hide();
    blocking.Show();

    WriteToInfo("Writing batch file...");
    WriteToInfo("");

    script += "@echo off\n";
    script += "\n";
    script += "REM Exit codes:\n";
    script += "REM   0 = Success\n";
    script += "REM   1 = Invalid arguments\n";
    script += "REM   2 = Folder does not exist\n";
    script += "REM   3 = No .exe files found\n";
    script += "REM   4 = One or more outbound rules failed\n";
    script += "REM   5 = One or more inbound rules failed\n";
    script += "REM   6 = One or more outbound and inbound rules failed\n";
    script += "\n";
    script += "REM Require exactly one argument\n";
    script += "if \"%~1\"==\"\" (\n";
    script += "    echo [ERROR] No folder path was specified.\n";
    script += "    exit 1\n";
    script += ")\n";
    script += "\n";
    script += "if not \"%~2\"==\"\" (\n";
    script += "    echo [ERROR] Too many arguments were specified.\n";
    script += "    exit 1\n";
    script += ")\n";
    script += "\n";
    script += "REM Get the folder path\n";
    script += "set \"folderPath=%~1\"\n";
    script += "\n";
    script += "REM Verify that the folder exists\n";
    script += "if not exist \"%folderPath%\\\" (\n";
    script += "    echo [ERROR] The specified folder does not exist: %folderPath%\n";
    script += "    exit 2\n";
    script += ")\n";
    script += "\n";
    script += "REM Extract the base folder name\n";
    script += "for %%A in (\"%folderPath%\") do set \"baseFolderName=%%~nA\"\n";
    script += "\n";
    script += "REM State flags\n";
    script += "set \"foundExe=0\"\n";
    script += "set \"outboundFailed=0\"\n";
    script += "set \"inboundFailed=0\"\n";
    script += "\n";
    script += "echo [INFO] Blocking all .exe files in folder: %folderPath%\n";
    script += "\n";
    script += "REM Process every .exe recursively.\n";
    script += "REM A failed firewall command does NOT stop the loop.\n";
    script += "for /r \"%folderPath%\" %%F in (*.exe) do (\n";
    script += "    set \"foundExe=1\"\n";
    script += "\n";
    script += "    echo [INFO] Adding outbound block rule for: %%F\n";
    script += "\n";
    script += "    netsh advfirewall firewall add rule ^\n";
    script += "        name=\"Block %baseFolderName% %%~nxF (automated) OUT\" ^\n";
    script += "        dir=out ^\n";
    script += "        program=\"%%F\" ^\n";
    script += "        action=block ^\n";
    script += "        enable=yes >nul\n";
    script += "\n";
    script += "    if errorlevel 1 (\n";
    script += "        echo [ERROR] Failed to add outbound firewall rule for: %%F\n";
    script += "        set \"outboundFailed=1\"\n";
    script += "    )\n";
    script += "\n";
    script += "    echo [INFO] Adding inbound block rule for: %%F\n";
    script += "\n";
    script += "    netsh advfirewall firewall add rule ^\n";
    script += "        name=\"Block %baseFolderName% %%~nxF (automated) IN\" ^\n";
    script += "        dir=in ^\n";
    script += "        program=\"%%F\" ^\n";
    script += "        action=block ^\n";
    script += "        enable=yes >nul\n";
    script += "\n";
    script += "    if errorlevel 1 (\n";
    script += "        echo [ERROR] Failed to add inbound firewall rule for: %%F\n";
    script += "        set \"inboundFailed=1\"\n";
    script += "    )\n";
    script += ")\n";
    script += "\n";
    script += "REM No executables were found\n";
    script += "if \"%foundExe%\"==\"0\" (\n";
    script += "    echo [ERROR] No .exe files were found in: %folderPath%\n";
    script += "    exit 3\n";
    script += ")\n";
    script += "\n";
    script += "REM Report aggregate firewall failures only after every file was attempted\n";
    script += "if \"%outboundFailed%\"==\"1\" (\n";
    script += "    if \"%inboundFailed%\"==\"1\" (\n";
    script += "        echo [ERROR] One or more outbound and inbound firewall rules could not be added.\n";
    script += "        exit 6\n";
    script += "    )\n";
    script += "\n";
    script += "    echo [ERROR] One or more outbound firewall rules could not be added.\n";
    script += "    exit 4\n";
    script += ")\n";
    script += "\n";
    script += "if \"%inboundFailed%\"==\"1\" (\n";
    script += "    echo [ERROR] One or more inbound firewall rules could not be added.\n";
    script += "    exit 5\n";
    script += ")\n";
    script += "\n";
    script += "echo [INFO] All .exe files in %folderPath% have been blocked successfully.\n";
    script += "exit 0\n";

    batFileName = Path.GetTempFileName();
    batFileName = Path.ChangeExtension(batFileName, ".bat");
    File.WriteAllText(batFileName, script);

    psi = new ProcessStartInfo();
    psi.FileName = "cmd.exe";
    psi.Arguments = $"/c \"\"{batFileName}\" \"{pathInput.Text}\"\"";
    psi.UseShellExecute = false;
    psi.CreateNoWindow = true;
    psi.RedirectStandardOutput = true;
    psi.RedirectStandardError = true;

    process = new Process();
    process.StartInfo = psi;
    process.EnableRaisingEvents = true;

    process.OutputDataReceived += (s, e) => BeginInvoke(() => {
      WriteToInfo(e.Data);
    });
    process.ErrorDataReceived += (s, e) => BeginInvoke(() => {
      WriteToInfo(e.Data);
    });
    process.Exited += (s, e) => BeginInvoke(() => {
      File.Delete(batFileName);
      this.FormClosing -= PreventQuit;
      ScriptExit(process.ExitCode);
      process.Dispose();
    });

    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
  }

  void WriteToInfo(string data)
  {
    if(data == null) return;
    blockingInfo.Text += data + Environment.NewLine;
  }

  void ScriptExit(int exitCode)
  {
    WriteToInfo("");
    WriteToInfo($"Exit Code: {exitCode}");

    blockingBack.Enabled = true;
    blockingClose.Enabled = true;
    if(exitCode == 0) blockingProgress.BackColor = progressCompleteColor;

    if(exitCode == 0) WriteToInfo("Todo bloqueado correctamente.");
    if(exitCode == 1) WriteToInfo("Error al parsear los argumentos.");
    if(exitCode == 2) WriteToInfo("Error al comprobar la existencia de la carpeta.");
    if(exitCode == 3) WriteToInfo("Nada para bloquear.");
    if(exitCode == 4) WriteToInfo("No se pudieron bloquear las conexiones salientes de uno o más archivos.");
    if(exitCode == 5) WriteToInfo("No se pudieron bloquear las conexiones entrantes de uno o más archivos.");
    if(exitCode == 6) WriteToInfo("No se pudieron bloquear una o más conexiones salientes y no se pudieron bloquear una o más conexiones entrantes.");

    if(exitCode == 1) MessageBox.Show("Error al parsear los argumentos. Este error debe ser solucionado por el programador.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    if(exitCode == 2) MessageBox.Show("Error al comprobar la existencia de la carpeta. Este error debe ser solucionado por el programador.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    if(exitCode == 5 || exitCode == 6) MessageBox.Show("No se pudieron bloquear las conexiones entrantes para uno o más archivos.", "Error Conexiones Entrantes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    if(exitCode == 4 || exitCode == 6) MessageBox.Show("No se pudieron bloquear las conexiones salientes para uno o más archivos.", "Error Conexiones salientes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
  }

  void Back()
  {
    blockingInfo.Text = "";
    blocking.Hide();
    main.Show();
  }

  void Quit()
  {
    Application.Exit();
  }

  void PreventQuit(object sender, FormClosingEventArgs e)
  {
    e.Cancel = true;
  }
}