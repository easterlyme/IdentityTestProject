using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityTestProject.ViewModels
{
    public class AuthorizeViewModel
    {
        public string ApplicationName { get; set; }

        public string RequestId { get; set; }

        public string Scope { get; set; }
    }
}
