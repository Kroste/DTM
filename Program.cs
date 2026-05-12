using NLog;
namespace DTM
{
    static class Program
    {

        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            try
            {
                ApplicationConfiguration.Initialize();
                Application.Run(new Main_Form());
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }
    }
}