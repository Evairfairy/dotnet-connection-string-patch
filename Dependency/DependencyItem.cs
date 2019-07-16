using System;
using System.Configuration;

namespace Dependency
{
    public class DependencyItem
    {
        public DependencyItem(Action<string> callback)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["default"].ConnectionString;
            callback(connectionString);
        }
    }
}