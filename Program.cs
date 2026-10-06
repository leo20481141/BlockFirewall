using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

static class Program
{
  [STAThread]
  static void Main()
  {
    WindowsIdentity identity = WindowsIdentity.GetCurrent();
    WindowsPrincipal principal = new WindowsPrincipal(identity);

    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    if(!principal.IsInRole(WindowsBuiltInRole.Administrator))
    {
      ProcessStartInfo psi = new ProcessStartInfo();
      psi.FileName = Application.ExecutablePath;
      psi.UseShellExecute = true;
      psi.Verb = "runas";
      try
      {
        Process.Start(psi);
        Application.Exit();
        return;
      }
      catch (System.ComponentModel.Win32Exception)
      {
        MessageBox.Show("La aplicación necesita permisos de administrador para funcionar. Si la aplicación no te pide los permisos, haz clic derecho y selecciona \"Ejecutar como administrador\"", "Error al iniciar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        Application.Exit();
        return;
      }
    }

    Application.Run(new MainForm());
  }
}
