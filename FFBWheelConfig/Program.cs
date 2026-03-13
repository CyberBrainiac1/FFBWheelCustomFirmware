using FFBWheelConfig.Forms;

namespace FFBWheelConfig;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}