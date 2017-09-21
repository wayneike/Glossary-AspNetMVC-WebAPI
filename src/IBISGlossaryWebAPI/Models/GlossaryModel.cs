using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GlossaryWebAPI.Models
{
    public class GlossaryModel
    {
        public string ID { get; set; }
        public string Term { get; set; }
        public string Definition { get; set; }
    }
}