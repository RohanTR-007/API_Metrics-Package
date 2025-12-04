using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace API_Metrics.UI
{
    public static class DashboardHtml
    {
        public static readonly string Content = Load();

        private static string Load()
        {
            var asm = Assembly.GetExecutingAssembly();
            var resourceName = asm.GetManifestResourceNames()
                .First(x => x.EndsWith("api-metric-dashboard.html", StringComparison.OrdinalIgnoreCase));

            using var stream = asm.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}

